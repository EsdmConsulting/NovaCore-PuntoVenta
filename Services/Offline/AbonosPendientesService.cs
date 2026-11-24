using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Credito;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Offline;

/// <summary>Abono a crédito recibido sin internet (o sin confirmar por el servidor).</summary>
public sealed class AbonoOffline
{
    public const string EstadoPendiente = "PENDIENTE";
    public const string EstadoEnviado = "ENVIADO";
    public const string EstadoRechazado = "RECHAZADO";

    /// <summary>También es el uuid_operacion: el servidor nunca lo aplica dos veces.</summary>
    public string Uuid { get; set; } = Guid.NewGuid().ToString();

    public string Estado { get; set; } = EstadoPendiente;
    public DateTime Fecha { get; set; } = DateTime.Now;

    public int IdCaja { get; set; }
    public int IdTurno { get; set; }
    public int IdCliente { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string FormaPago { get; set; } = string.Empty;

    public RegistrarAbonoCreditoRequest Request { get; set; } = new();

    public int Intentos { get; set; }
    public string UltimoError { get; set; } = string.Empty;
    public DateTime? FechaEnvio { get; set; }
    public int? IdAbonoServidor { get; set; }

    public List<AplicacionDocumentoCredito> Aplicaciones => Request.Aplicaciones;
    public decimal Total => Request.Aplicaciones.Sum(a => a.Monto);
}


/// <summary>
/// Cola de abonos sin conexión (abonos-pendientes/ → abonos-enviados/).
/// Se envían con el mismo uuid_operacion a api/comercial/credito/registrar-abono,
/// que ya es idempotente: reenviar nunca duplica el abono.
/// </summary>
public static class AbonosPendientesService
{
    public static event Action? CambioCola;

    private static string CarpetaPendientes => Carpeta("abonos-pendientes");
    private static string CarpetaEnviados => Carpeta("abonos-enviados");

    private static string Carpeta(string nombre)
    {
        var ruta = Path.Combine(AlmacenLocal.Directorio, nombre);
        Directory.CreateDirectory(ruta);
        return ruta;
    }

    private static string Nombre(AbonoOffline abono) =>
        $"{abono.Fecha:yyyyMMdd-HHmmssfff}-{abono.Uuid}.json";

    private static string RutaPendiente(AbonoOffline abono) =>
        Path.Combine(CarpetaPendientes, Nombre(abono));


    /// <summary>Guarda el abono en la caja; se envía en cuanto haya internet.</summary>
    public static AbonoOffline Encolar(RegistrarAbonoCreditoRequest request, string nombreCliente)
    {
        request.UuidOperacion ??= Guid.NewGuid().ToString();

        var abono = new AbonoOffline
        {
            Uuid = request.UuidOperacion,
            IdCaja = request.IdCaja ?? 0,
            IdTurno = request.IdTurno ?? 0,
            IdCliente = request.IdCliente,
            NombreCliente = nombreCliente,
            FormaPago = request.FormaPago,
            Request = request
        };

        AlmacenLocal.Guardar(RutaPendiente(abono), abono);
        CambioCola?.Invoke();
        _ = VentasPendientesService.EnviarPendientesAsync();
        return abono;
    }


    private static IEnumerable<AbonoOffline> Leer(string carpeta) =>
        Directory.EnumerateFiles(carpeta, "*.json")
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => AlmacenLocal.Leer<AbonoOffline>(f))
            .Where(a => a is not null)
            .Cast<AbonoOffline>();

    public static List<AbonoOffline> ObtenerPendientes() => Leer(CarpetaPendientes).ToList();

    public static List<AbonoOffline> ObtenerDelCliente(int idCliente) =>
        Leer(CarpetaPendientes).Concat(Leer(CarpetaEnviados))
            .Where(a => a.IdCliente == idCliente)
            .ToList();

    public static List<AbonoOffline> ObtenerDelTurno(int idTurno) =>
        Leer(CarpetaPendientes).Concat(Leer(CarpetaEnviados))
            .Where(a => a.IdTurno == idTurno && a.Estado != AbonoOffline.EstadoRechazado)
            .ToList();

    public static int ContarPorEnviar(int? idTurno = null) =>
        Leer(CarpetaPendientes).Count(a =>
            a.Estado == AbonoOffline.EstadoPendiente &&
            (idTurno is null || a.IdTurno == idTurno));

    public static int ContarRechazados() =>
        Leer(CarpetaPendientes).Count(a => a.Estado == AbonoOffline.EstadoRechazado);


    public static void LimpiarEnviadosAntiguos()
    {
        try
        {
            var limite = DateTime.Now.AddDays(-30);
            foreach (var archivo in Directory.EnumerateFiles(CarpetaEnviados, "*.json"))
            {
                if (File.GetLastWriteTime(archivo) < limite)
                    File.Delete(archivo);
            }
        }
        catch
        {
            // No es crítico.
        }
    }


    // =========================================================
    // ENVÍO (lo llama VentasPendientesService, después de las ventas)
    // =========================================================

    /// <summary>false si se cortó por falta de conexión o sesión.</summary>
    internal static async Task<bool> EnviarPendientesAsync(Func<Task<bool>> reautenticar)
    {
        foreach (var abono in ObtenerPendientes())
        {
            if (abono.Estado != AbonoOffline.EstadoPendiente)
                continue;

            abono.Intentos++;

            try
            {
                var (status, respuesta) = await PublicarAsync(abono);

                if (status == HttpStatusCode.Unauthorized && await reautenticar())
                    (status, respuesta) = await PublicarAsync(abono);

                if (status == HttpStatusCode.Unauthorized)
                {
                    Guardar(abono, "La sesión expiró.");
                    return false;
                }

                if (respuesta?.Res == 1)
                {
                    abono.Estado = AbonoOffline.EstadoEnviado;
                    abono.FechaEnvio = DateTime.Now;
                    abono.IdAbonoServidor = respuesta.Data?.IdAbono;
                    abono.UltimoError = string.Empty;

                    AlmacenLocal.Guardar(Path.Combine(CarpetaEnviados, Nombre(abono)), abono);
                    AlmacenLocal.Borrar(RutaPendiente(abono));
                    CambioCola?.Invoke();
                    continue;
                }

                var mensaje = respuesta?.Msg ?? $"El servidor respondió {(int)status}.";

                // Igual que las ventas: rechazo definitivo solo con respuesta del
                // servicio y un 4xx (no 404/408/429).
                if (respuesta is not null &&
                    (int)status is >= 400 and < 500 and not 404 and not 408 and not 429)
                {
                    abono.Estado = AbonoOffline.EstadoRechazado;
                }

                Guardar(abono, mensaje);
            }
            catch (Exception ex) when (ConexionService.EsErrorDeRed(ex))
            {
                ConexionService.ReportarFalloDeRed();
                Guardar(abono, "Sin conexión.");
                return false;
            }
            catch (Exception ex)
            {
                Guardar(abono, ex.Message);
            }
        }

        return true;
    }


    private static void Guardar(AbonoOffline abono, string error)
    {
        abono.UltimoError = error;
        AlmacenLocal.Guardar(RutaPendiente(abono), abono);
        CambioCola?.Invoke();
    }


    private static async Task<(HttpStatusCode, RegistrarAbonoCreditoResponse?)> PublicarAsync(AbonoOffline abono)
    {
        abono.Request.UuidOperacion = abono.Uuid;

        using var cancelacion = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var response = await ApiClient.Http.PostAsJsonAsync(
            "api/comercial/credito/registrar-abono", abono.Request, cancelacion.Token);

        var contenido = await response.Content.ReadAsStringAsync(cancelacion.Token);
        var inicio = contenido.IndexOf('{');

        RegistrarAbonoCreditoResponse? respuesta = null;
        if (inicio >= 0)
        {
            try
            {
                respuesta = JsonSerializer.Deserialize<RegistrarAbonoCreditoResponse>(
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
