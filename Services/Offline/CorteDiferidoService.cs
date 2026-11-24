using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Turno;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Offline;

/// <summary>Corte hecho sin internet: se cierra en el servidor al volver la conexión.</summary>
public sealed class CorteDiferido
{
    public const string EstadoPendiente = "PENDIENTE";
    public const string EstadoCerrado = "CERRADO";
    public const string EstadoRechazado = "RECHAZADO";

    public int IdTurno { get; set; }
    public int IdCaja { get; set; }
    public string Cajero { get; set; } = string.Empty;
    public decimal EfectivoDeclarado { get; set; }
    public string Observaciones { get; set; } = string.Empty;

    /// <summary>Hora real del conteo: es la hora de cierre del turno en el servidor.</summary>
    public DateTime Fecha { get; set; } = DateTime.Now;

    public string Estado { get; set; } = EstadoPendiente;
    public int Intentos { get; set; }
    public string UltimoError { get; set; } = string.Empty;
    public DateTime? FechaEnvio { get; set; }
}


/// <summary>
/// Cola de cortes diferidos (cortes-pendientes/ → cortes-cerrados/). Se
/// cierran después de enviar todas las ventas y abonos de ese turno, con
/// api/pos/turnos/{id}/cerrar (fecha_cierre real y diferido = true, que
/// en el servidor es idempotente).
/// </summary>
public static class CorteDiferidoService
{
    public static event Action<CorteDiferido>? CorteCerrado;

    private static string CarpetaPendientes => Carpeta("cortes-pendientes");
    private static string CarpetaCerrados => Carpeta("cortes-cerrados");

    private static string Carpeta(string nombre)
    {
        var ruta = Path.Combine(AlmacenLocal.Directorio, nombre);
        Directory.CreateDirectory(ruta);
        return ruta;
    }

    private static string Nombre(CorteDiferido c) => $"turno-{c.IdTurno}.json";


    public static void Registrar(CorteDiferido corte)
    {
        corte.Estado = CorteDiferido.EstadoPendiente;
        AlmacenLocal.Guardar(Path.Combine(CarpetaPendientes, Nombre(corte)), corte);
        _ = VentasPendientesService.EnviarPendientesAsync();
    }

    public static List<CorteDiferido> ObtenerPendientes() =>
        Directory.EnumerateFiles(CarpetaPendientes, "*.json")
            .Select(f => AlmacenLocal.Leer<CorteDiferido>(f))
            .Where(c => c is not null)
            .Cast<CorteDiferido>()
            .OrderBy(c => c.Fecha)
            .ToList();

    /// <summary>El turno ya tiene un corte hecho sin conexión (no se puede seguir usando).</summary>
    public static bool TieneCortePendiente(int idTurno) =>
        ObtenerPendientes().Any(c => c.IdTurno == idTurno && c.Estado == CorteDiferido.EstadoPendiente);

    public static int ContarPorEnviar() =>
        ObtenerPendientes().Count(c => c.Estado == CorteDiferido.EstadoPendiente);

    public static int ContarRechazados() =>
        ObtenerPendientes().Count(c => c.Estado == CorteDiferido.EstadoRechazado);


    /// <summary>
    /// Cierra en el servidor los cortes cuyo turno ya no tiene ventas ni
    /// abonos por enviar. false si se cortó por falta de conexión o sesión.
    /// </summary>
    internal static async Task<bool> EnviarPendientesAsync(Func<Task<bool>> reautenticar)
    {
        foreach (var corte in ObtenerPendientes())
        {
            if (corte.Estado != CorteDiferido.EstadoPendiente)
                continue;

            // Primero tienen que llegar todas las operaciones del turno.
            if (VentasPendientesService.ContarPorEnviar(corte.IdTurno) > 0 ||
                AbonosPendientesService.ContarPorEnviar(corte.IdTurno) > 0)
                continue;

            corte.Intentos++;

            try
            {
                var (status, respuesta) = await PublicarAsync(corte);

                if (status == HttpStatusCode.Unauthorized && await reautenticar())
                    (status, respuesta) = await PublicarAsync(corte);

                if (status == HttpStatusCode.Unauthorized)
                {
                    Guardar(corte, "La sesión expiró: el corte se cerrará al iniciar sesión con internet.");
                    return false;
                }

                if (respuesta?.Res == 1)
                {
                    corte.Estado = CorteDiferido.EstadoCerrado;
                    corte.FechaEnvio = DateTime.Now;
                    corte.UltimoError = string.Empty;

                    AlmacenLocal.Guardar(Path.Combine(CarpetaCerrados, Nombre(corte)), corte);
                    AlmacenLocal.Borrar(Path.Combine(CarpetaPendientes, Nombre(corte)));
                    CorteCerrado?.Invoke(corte);
                    continue;
                }

                var mensaje = respuesta?.Msg ?? $"El servidor respondió {(int)status}.";

                if (respuesta is not null &&
                    (int)status is >= 400 and < 500 and not 404 and not 408 and not 429)
                {
                    corte.Estado = CorteDiferido.EstadoRechazado;
                }

                Guardar(corte, mensaje);
            }
            catch (Exception ex) when (ConexionService.EsErrorDeRed(ex))
            {
                ConexionService.ReportarFalloDeRed();
                Guardar(corte, "Sin conexión.");
                return false;
            }
            catch (Exception ex)
            {
                Guardar(corte, ex.Message);
            }
        }

        return true;
    }


    private static void Guardar(CorteDiferido corte, string error)
    {
        corte.UltimoError = error;
        AlmacenLocal.Guardar(Path.Combine(CarpetaPendientes, Nombre(corte)), corte);
    }


    private static async Task<(HttpStatusCode, CerrarTurnoResponse?)> PublicarAsync(CorteDiferido corte)
    {
        var cuerpo = new
        {
            id_turno = corte.IdTurno,
            efectivo_declarado = corte.EfectivoDeclarado,
            observaciones = string.IsNullOrWhiteSpace(corte.Observaciones)
                ? "Corte realizado sin conexión."
                : $"{corte.Observaciones} (corte realizado sin conexión)",
            fecha_cierre = corte.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
            diferido = true
        };

        using var cancelacion = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var response = await ApiClient.Http.PostAsJsonAsync(
            $"api/pos/turnos/{corte.IdTurno}/cerrar", cuerpo, cancelacion.Token);

        var contenido = await response.Content.ReadAsStringAsync(cancelacion.Token);
        var inicio = contenido.IndexOf('{');

        CerrarTurnoResponse? respuesta = null;
        if (inicio >= 0)
        {
            try
            {
                respuesta = JsonSerializer.Deserialize<CerrarTurnoResponse>(
                    contenido[inicio..],
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                    });
            }
            catch (JsonException)
            {
                // Respuesta no JSON: error temporal.
            }
        }

        return (response.StatusCode, respuesta);
    }
}
