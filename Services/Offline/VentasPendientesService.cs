using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NovaCoreESDM.Services.Api;
using NovaCoreESDM.Services.Auth;
using NovaCoreESDM.Services.Ventas;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Cola de ventas hechas sin internet:
///  - borrador-{caja}.json  → la venta que se está capturando (se recupera tras un apagón)
///  - pendientes/*.json     → ventas cobradas que faltan por enviar
///  - enviadas/*.json       → ventas ya recibidas por el servidor (historial)
/// Cuando hay internet las envía solas, en orden, una por una.
/// </summary>
public static class VentasPendientesService
{
    private static readonly SemaphoreSlim _enviando = new(1, 1);
    private static Timer? _temporizador;

    /// <summary>Se dispara (en segundo plano) cuando cambia la cola.</summary>
    public static event Action? CambioCola;

    /// <summary>Se dispara cuando el servidor rechaza una venta.</summary>
    public static event Action<VentaOffline>? VentaRechazada;

    public static bool EstaEnviando { get; private set; }

    public static string UltimoResultado { get; private set; } = string.Empty;


    private static string CarpetaPendientes => Carpeta("pendientes");
    private static string CarpetaEnviadas => Carpeta("enviadas");

    private static string Carpeta(string nombre)
    {
        var ruta = Path.Combine(AlmacenLocal.Directorio, nombre);
        Directory.CreateDirectory(ruta);
        return ruta;
    }

    private static string RutaPendiente(VentaOffline venta) =>
        Path.Combine(CarpetaPendientes,
            $"{venta.FechaCreacion:yyyyMMdd-HHmmssfff}-{venta.Uuid}.json");


    // =========================================================
    // BORRADOR (venta en captura)
    // =========================================================

    private static string RutaBorrador(int idCaja) =>
        AlmacenLocal.Ruta($"borrador-{idCaja}.json");

    public static void GuardarBorrador(VentaOffline venta) =>
        AlmacenLocal.Guardar(RutaBorrador(venta.IdCaja), venta);

    public static VentaOffline? ObtenerBorrador(int idCaja) =>
        AlmacenLocal.Leer<VentaOffline>(RutaBorrador(idCaja));

    public static void BorrarBorrador(int idCaja) =>
        AlmacenLocal.Borrar(RutaBorrador(idCaja));


    // =========================================================
    // BORRADORES ABANDONADOS EN EL SERVIDOR
    // =========================================================
    //
    // Si se va el internet con una venta del servidor a medias, esa venta
    // se continúa en la caja y el borrador del servidor (que tiene
    // inventario apartado) se cancela en cuanto regresa la conexión.

    private static string RutaAbandonados => AlmacenLocal.Ruta("borradores-abandonados.json");

    public static void RegistrarBorradorAbandonado(int idVenta)
    {
        var ids = ObtenerBorradoresAbandonados();
        if (!ids.Contains(idVenta))
        {
            ids.Add(idVenta);
            AlmacenLocal.Guardar(RutaAbandonados, ids);
        }
    }

    public static List<int> ObtenerBorradoresAbandonados() =>
        AlmacenLocal.Leer<List<int>>(RutaAbandonados) ?? new List<int>();

    private static async Task<bool> CancelarBorradoresAbandonadosAsync()
    {
        var ids = ObtenerBorradoresAbandonados();
        if (ids.Count == 0)
            return true;

        var servicio = new VentasService();

        foreach (var id in ids.ToList())
        {
            var resultado = await servicio.CancelarVentaAsync(id);

            if (!ConexionService.EstaEnLinea)
                return false;

            // Cancelado, o ya no existía / ya no era borrador: se olvida.
            Console.WriteLine($"Borrador abandonado {id}: {resultado.Res} {resultado.Msg}");
            ids.Remove(id);
            AlmacenLocal.Guardar(RutaAbandonados, ids);
        }

        return true;
    }


    // =========================================================
    // COLA
    // =========================================================

    /// <summary>
    /// Pasa la venta cobrada a la cola de envío. Primero se escribe la
    /// venta pendiente y después se borra el borrador: si se va la luz
    /// entre los dos pasos, al volver se detecta que ya estaba cobrada.
    /// </summary>
    public static void Encolar(VentaOffline venta)
    {
        venta.Estado = VentaOffline.EstadoPendiente;
        AlmacenLocal.Guardar(RutaPendiente(venta), venta);
        BorrarBorrador(venta.IdCaja);
        CambioCola?.Invoke();
        _ = EnviarPendientesAsync();
    }

    public static bool EstaEncolada(string uuid) =>
        Directory.EnumerateFiles(CarpetaPendientes, $"*-{uuid}.json").Any()
        || Directory.EnumerateFiles(CarpetaEnviadas, $"*-{uuid}.json").Any();


    public static List<VentaOffline> ObtenerPendientes() =>
        Directory.EnumerateFiles(CarpetaPendientes, "*.json")
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => AlmacenLocal.Leer<VentaOffline>(f))
            .Where(v => v is not null)
            .Cast<VentaOffline>()
            .ToList();


    public static int ContarPorEnviar(int? idTurno = null, int? idCaja = null) =>
        ObtenerPendientes().Count(v =>
            v.Estado == VentaOffline.EstadoPendiente &&
            (idTurno is null || v.IdTurno == idTurno) &&
            (idCaja is null || v.IdCaja == idCaja));


    public static int ContarRechazadas() =>
        ObtenerPendientes().Count(v => v.Estado == VentaOffline.EstadoRechazada);


    /// <summary>Ventas cobradas sin conexión de este turno (pendientes o ya enviadas).</summary>
    public static List<VentaOffline> ObtenerDelTurno(int idTurno) =>
        ObtenerPendientes()
            .Concat(Directory.EnumerateFiles(CarpetaEnviadas, "*.json")
                .Select(f => AlmacenLocal.Leer<VentaOffline>(f))
                .Where(v => v is not null)
                .Cast<VentaOffline>())
            .Where(v => v.IdTurno == idTurno)
            .OrderBy(v => v.FechaCobro ?? v.FechaCreacion)
            .ToList();


    /// <summary>Vuelve a poner en cola una venta rechazada (después de corregir el problema).</summary>
    public static void Reintentar(string uuid)
    {
        foreach (var venta in ObtenerPendientes().Where(v => v.Uuid == uuid))
        {
            venta.Estado = VentaOffline.EstadoPendiente;
            venta.UltimoError = string.Empty;
            AlmacenLocal.Guardar(RutaPendiente(venta), venta);
        }

        CambioCola?.Invoke();
        _ = EnviarPendientesAsync();
    }


    // =========================================================
    // ENVÍO AUTOMÁTICO
    // =========================================================

    public static void Iniciar()
    {
        LimpiarEnviadasAntiguas();

        ConexionService.CambioEstado += enLinea =>
        {
            if (enLinea)
                _ = EnviarPendientesAsync();
        };

        // Respaldo por si algo quedó atorado: reintenta cada minuto.
        _temporizador ??= new Timer(
            _ => { if (ConexionService.EstaEnLinea) _ = EnviarPendientesAsync(); },
            null, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1));

        ConexionService.Iniciar();
    }


    /// <summary>El historial de ventas ya enviadas se conserva 30 días.</summary>
    private static void LimpiarEnviadasAntiguas()
    {
        try
        {
            var limite = DateTime.Now.AddDays(-30);
            foreach (var archivo in Directory.EnumerateFiles(CarpetaEnviadas, "*.json"))
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


    public static async Task EnviarPendientesAsync()
    {
        if (!await _enviando.WaitAsync(0))
            return;

        EstaEnviando = true;
        CambioCola?.Invoke();

        try
        {
            if (!await CancelarBorradoresAbandonadosAsync())
                return;

            foreach (var venta in ObtenerPendientes())
            {
                if (venta.Estado != VentaOffline.EstadoPendiente)
                    continue;

                var resultado = await EnviarAsync(venta);

                if (resultado == ResultadoEnvio.SinConexion)
                    break;

                if (resultado == ResultadoEnvio.SesionVencida)
                {
                    UltimoResultado = "Inicia sesión de nuevo para enviar las ventas pendientes.";
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando ventas sin conexión: {ex}");
        }
        finally
        {
            EstaEnviando = false;
            _enviando.Release();
            CambioCola?.Invoke();
        }
    }


    private enum ResultadoEnvio { Enviada, Rechazada, ErrorTemporal, SinConexion, SesionVencida }


    private static async Task<ResultadoEnvio> EnviarAsync(VentaOffline venta)
    {
        venta.Intentos++;

        try
        {
            var (status, respuesta) = await PublicarAsync(venta);

            if (status == HttpStatusCode.Unauthorized && await ReautenticarAsync())
                (status, respuesta) = await PublicarAsync(venta);

            if (status == HttpStatusCode.Unauthorized)
                return Guardar(venta, ResultadoEnvio.SesionVencida, "La sesión expiró.");

            if (respuesta?.Res == 1 && respuesta.Data is not null)
            {
                venta.Estado = VentaOffline.EstadoEnviada;
                venta.FechaEnvio = DateTime.Now;
                venta.IdVentaServidor = respuesta.Data.IdVenta;
                venta.FolioTicketServidor = respuesta.Data.FolioTicket;

                ConsecutivoTicketLocal.Aprender(
                    venta.IdCaja,
                    respuesta.Data.CodigoUnidadOperativa,
                    respuesta.Data.SerieTicket,
                    respuesta.Data.NumeroTicket);
                venta.Avisos = respuesta.Data.Avisos.Select(a => a.Msg).ToList();
                venta.UltimoError = string.Empty;

                var destino = Path.Combine(CarpetaEnviadas, Path.GetFileName(RutaPendiente(venta)));
                AlmacenLocal.Guardar(destino, venta);
                AlmacenLocal.Borrar(RutaPendiente(venta));

                UltimoResultado = $"Venta {venta.Folio} enviada como {venta.FolioTicketServidor}.";
                return ResultadoEnvio.Enviada;
            }

            var mensaje = respuesta?.Msg ?? $"El servidor respondió {(int)status}.";

            // Rechazo definitivo solo si el servicio respondió con su formato
            // (res = 0) y un 4xx. Un 404 sin respuesta del servicio = todavía
            // no está publicado en el servidor: se queda esperando.
            var definitivo =
                respuesta is not null &&
                (int)status is >= 400 and < 500 and not 404 and not 408 and not 429;

            if (definitivo)
            {
                venta.Estado = VentaOffline.EstadoRechazada;
                var r = Guardar(venta, ResultadoEnvio.Rechazada, mensaje);
                VentaRechazada?.Invoke(venta);
                return r;
            }

            return Guardar(venta, ResultadoEnvio.ErrorTemporal, mensaje);
        }
        catch (Exception ex) when (ConexionService.EsErrorDeRed(ex))
        {
            ConexionService.ReportarFalloDeRed();
            return Guardar(venta, ResultadoEnvio.SinConexion, "Sin conexión.");
        }
        catch (Exception ex)
        {
            return Guardar(venta, ResultadoEnvio.ErrorTemporal, ex.Message);
        }
    }


    private static ResultadoEnvio Guardar(VentaOffline venta, ResultadoEnvio resultado, string error)
    {
        venta.UltimoError = error;
        AlmacenLocal.Guardar(RutaPendiente(venta), venta);
        UltimoResultado = error;
        return resultado;
    }


    private static async Task<(HttpStatusCode, RespuestaSincronizar?)> PublicarAsync(VentaOffline venta)
    {
        var cuerpo = new
        {
            uuid = venta.Uuid,
            folio = venta.Folio,
            id_empresa = venta.IdEmpresa,
            id_unidad_operativa = venta.IdUnidadOperativa,
            id_caja = venta.IdCaja,
            id_turno = venta.IdTurno,
            id_usuario = venta.IdUsuario,
            usuario = venta.Usuario,
            tipo_venta = "CONTADO",
            serie_ticket = venta.SerieTicket,
            numero_ticket = venta.NumeroTicket,
            fecha_hora = (venta.FechaCobro ?? venta.FechaCreacion).ToString("yyyy-MM-dd HH:mm:ss"),
            recibido = venta.Recibido,
            cambio = venta.Cambio,
            lineas = venta.Lineas.Select(l => new
            {
                id_producto = l.IdProducto,
                id_presentacion = l.IdPresentacion,
                cantidad_comercial = l.Cantidad,
                factor_conversion = l.FactorConversion,
                precio_unitario = l.PrecioUnitario,
                tipo_precio = l.TipoPrecio,
                descripcion_snapshot = $"{l.Nombre} - {l.NombrePresentacion}"
            }),
            pagos = venta.Pagos.Select(p => new
            {
                id_forma_pago = p.IdFormaPago,
                importe = p.Importe,
                referencia = p.Referencia,
                autorizacion = p.Autorizacion
            })
        };

        using var cancelacion = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var response = await ApiClient.Http.PostAsJsonAsync(
            "api/pos/ventas/sincronizar", cuerpo, cancelacion.Token);

        var contenido = await response.Content.ReadAsStringAsync(cancelacion.Token);
        var inicio = contenido.IndexOf('{');

        RespuestaSincronizar? respuesta = null;
        if (inicio >= 0)
        {
            try
            {
                respuesta = JsonSerializer.Deserialize<RespuestaSincronizar>(
                    contenido[inicio..], AlmacenLocal.Opciones);
            }
            catch (JsonException)
            {
                // Respuesta no JSON: se trata como error temporal del servidor.
            }
        }

        return (response.StatusCode, respuesta);
    }


    /// <summary>
    /// La sesión PHP del servidor caduca. Si la caja tiene en memoria la
    /// contraseña del cajero (de este inicio de sesión), vuelve a entrar.
    /// </summary>
    private static async Task<bool> ReautenticarAsync()
    {
        var (usuario, contrasena) = SesionLocal.CredencialesEnMemoria;
        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
            return false;

        var login = await new AuthService().LoginAsync(usuario, contrasena);
        return login.Res == 1;
    }


    private sealed class RespuestaSincronizar
    {
        [System.Text.Json.Serialization.JsonPropertyName("res")]
        public int Res { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public DatosSincronizar? Data { get; set; }
    }

    private sealed class DatosSincronizar
    {
        [System.Text.Json.Serialization.JsonPropertyName("id_venta")]
        public int IdVenta { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("folio_ticket")]
        public string FolioTicket { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("serie_ticket")]
        public string SerieTicket { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("numero_ticket")]
        public long NumeroTicket { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("codigo_unidad_operativa")]
        public string CodigoUnidadOperativa { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("ya_registrada")]
        public bool YaRegistrada { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("avisos")]
        public List<AvisoSincronizar> Avisos { get; set; } = new();
    }

    private sealed class AvisoSincronizar
    {
        [System.Text.Json.Serialization.JsonPropertyName("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;
    }
}
