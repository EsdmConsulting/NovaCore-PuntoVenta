using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Clientes;
using NovaCoreESDM.Models.Credito;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Copia en la caja de los clientes con crédito (estado y documentos
/// pendientes), descargada con internet desde api/pos/clientes/credito-local.
/// Sin internet permite buscar al cliente, venderle a crédito y recibir sus
/// abonos. A lo descargado se le descuenta lo que esta caja hizo después
/// (ventas a crédito y abonos sin conexión) para no prestar de más.
/// </summary>
public static class CreditoLocal
{
    /// <summary>Más vieja que esto, la copia ya no sirve para vender a crédito.</summary>
    public static readonly TimeSpan VigenciaParaVender = TimeSpan.FromHours(48);

    private static readonly SemaphoreSlim _descargando = new(1, 1);
    private static readonly object _candado = new();
    private static Copia? _copia;
    private static bool _leida;

    private static string Ruta => AlmacenLocal.Ruta("credito-clientes.json");


    public sealed class Copia
    {
        public DateTime Descargado { get; set; }
        public List<ClienteCredito> Clientes { get; set; } = new();
    }

    public sealed class ClienteCredito
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("codigo")] public string Codigo { get; set; } = string.Empty;
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;
        [JsonPropertyName("rfc")] public string Rfc { get; set; } = string.Empty;
        [JsonPropertyName("estado_credito")] public EstadoCreditoData? EstadoCredito { get; set; }
        [JsonPropertyName("documentos_pendientes")] public List<DocumentoCreditoPendiente> DocumentosPendientes { get; set; } = new();
    }

    private sealed class Respuesta
    {
        [JsonPropertyName("res")] public int Res { get; set; }
        [JsonPropertyName("msg")] public string Msg { get; set; } = string.Empty;
        [JsonPropertyName("data")] public DatosRespuesta? Data { get; set; }
    }

    private sealed class DatosRespuesta
    {
        [JsonPropertyName("clientes")] public List<ClienteCredito> Clientes { get; set; } = new();
    }

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };


    private static Copia? Actual
    {
        get
        {
            lock (_candado)
            {
                if (!_leida)
                {
                    _copia = AlmacenLocal.Leer<Copia>(Ruta);
                    _leida = true;
                }

                return _copia;
            }
        }
    }

    public static DateTime? FechaCopia => Actual?.Descargado;


    // =========================================================
    // DESCARGA (con internet)
    // =========================================================

    /// <summary>Descarga la copia. No falla: si no se puede, se queda la anterior.</summary>
    public static async Task DescargarAsync()
    {
        if (!ConexionService.EstaEnLinea || !await _descargando.WaitAsync(0))
            return;

        try
        {
            using var cancelacion = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var response = await ApiClient.Http.GetAsync(
                "api/pos/clientes/credito-local", cancelacion.Token);

            var contenido = await response.Content.ReadAsStringAsync(cancelacion.Token);
            var inicio = contenido.IndexOf('{');

            if (!response.IsSuccessStatusCode || inicio < 0)
                return;

            var respuesta = JsonSerializer.Deserialize<Respuesta>(contenido[inicio..], OpcionesJson);

            if (respuesta?.Res != 1 || respuesta.Data is null)
                return;

            var copia = new Copia
            {
                Descargado = DateTime.Now,
                Clientes = respuesta.Data.Clientes
            };

            AlmacenLocal.Guardar(Ruta, copia);

            lock (_candado)
            {
                _copia = copia;
                _leida = true;
            }
        }
        catch (Exception ex)
        {
            if (ConexionService.EsErrorDeRed(ex))
                ConexionService.ReportarFalloDeRed();

            Console.WriteLine($"No se pudo descargar la copia de clientes con crédito: {ex.Message}");
        }
        finally
        {
            _descargando.Release();
        }
    }


    // =========================================================
    // BÚSQUEDA
    // =========================================================

    public static List<ClientePos> Buscar(string texto, int maximo = 10)
    {
        var copia = Actual;
        texto = (texto ?? string.Empty).Trim();

        if (copia is null || texto.Length < 2)
            return new List<ClientePos>();

        return copia.Clientes
            .Where(c =>
                c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                c.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                c.Rfc.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Nombre.StartsWith(texto, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(c => c.Nombre)
            .Take(maximo)
            .Select(c => new ClientePos
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Rfc = c.Rfc,
                DiasCredito = c.EstadoCredito?.DiasCredito,
                Estatus = 1
            })
            .ToList();
    }


    // =========================================================
    // ESTADO DE CRÉDITO SIN CONEXIÓN
    // =========================================================

    /// <summary>
    /// Lo que esta caja le cargó al cliente después de descargar la copia:
    /// ventas a crédito sin conexión por enviar, o enviadas después.
    /// </summary>
    private static decimal CreditoUsadoDespues(int idCliente, DateTime descargado) =>
        VentasPendientesService.ObtenerCreditoLocal(idCliente)
            .Where(v => v.Estado != VentaOffline.EstadoEnviada || v.FechaEnvio is null || v.FechaEnvio > descargado)
            .Sum(v => v.Total);


    /// <summary>Abonos de esta caja que la copia todavía no refleja.</summary>
    private static List<AbonoOffline> AbonosDespues(int idCliente, DateTime descargado) =>
        AbonosPendientesService.ObtenerDelCliente(idCliente)
            .Where(a => a.Estado != AbonoOffline.EstadoEnviado || a.FechaEnvio is null || a.FechaEnvio > descargado)
            .ToList();


    /// <summary>
    /// Estado del crédito con la copia local, para una venta del monto dado.
    /// null si el cliente no está en la copia.
    /// </summary>
    public static EstadoCreditoData? Estado(int idCliente, decimal montoNuevaVenta)
    {
        var copia = Actual;
        var cliente = copia?.Clientes.FirstOrDefault(c => c.Id == idCliente);

        if (copia is null || cliente?.EstadoCredito is null)
            return null;

        var b = cliente.EstadoCredito;
        var usado = CreditoUsadoDespues(idCliente, copia.Descargado);

        // Los abonos sin conexión no liberan crédito hasta que el servidor
        // los aplique (más seguro).
        var disponible = Math.Max(0m, b.Disponible - usado);
        var motivos = new List<string>(b.MotivosBloqueo);

        if (!b.ManejaCredito)
            motivos.Add("El cliente no tiene una línea de crédito activa.");

        if (DateTime.Now - copia.Descargado > VigenciaParaVender)
            motivos.Add($"La información de crédito de esta caja es del {copia.Descargado:dd/MM/yyyy HH:mm}; se necesita internet para actualizarla.");

        if (montoNuevaVenta > disponible)
            motivos.Add($"El crédito disponible (${disponible:N2}) no alcanza para esta venta (${montoNuevaVenta:N2}).");

        var puede = b.PuedeVenderCredito && motivos.Count == 0;

        return new EstadoCreditoData
        {
            IdCliente = b.IdCliente,
            IdCredito = b.IdCredito,
            IdAnalisisCredito = b.IdAnalisisCredito,
            LimiteCredito = b.LimiteCredito,
            SaldoDeudor = b.SaldoDeudor + usado,
            SaldoVencido = b.SaldoVencido,
            DiasMaxVencido = b.DiasMaxVencido,
            DiasCredito = b.DiasCredito,
            ToleranciaDias = b.ToleranciaDias,
            FechaInicio = b.FechaInicio,
            FechaVencimiento = b.FechaVencimiento,
            Disponible = disponible,
            MontoNuevaVenta = montoNuevaVenta,
            SaldoProyectado = b.SaldoDeudor + usado + montoNuevaVenta,
            DisponibleProyectado = disponible - montoNuevaVenta,
            PuedeVenderCredito = puede,
            Semaforo = puede ? b.Semaforo : "ROJO",
            MotivosBloqueo = puede ? new List<string>() : motivos.Distinct().ToList(),
            ManejaCredito = b.ManejaCredito
        };
    }


    /// <summary>
    /// Resumen del cliente con la copia local: documentos pendientes menos
    /// los abonos sin conexión que esta caja ya recibió.
    /// </summary>
    public static ResumenCreditoData? Resumen(int idCliente)
    {
        var copia = Actual;
        var cliente = copia?.Clientes.FirstOrDefault(c => c.Id == idCliente);

        if (copia is null || cliente is null)
            return null;

        var aplicado = AbonosDespues(idCliente, copia.Descargado)
            .SelectMany(a => a.Aplicaciones)
            .GroupBy(a => a.IdDocumentoCredito)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.Monto));

        var documentos = cliente.DocumentosPendientes
            .Select(d =>
            {
                var abonado = aplicado.GetValueOrDefault(d.Id);
                return new DocumentoCreditoPendiente
                {
                    Id = d.Id,
                    TipoDocumento = d.TipoDocumento,
                    Folio = d.Folio,
                    UuidCfdi = d.UuidCfdi,
                    MetodoPago = d.MetodoPago,
                    FechaEmision = d.FechaEmision,
                    FechaVencimiento = d.FechaVencimiento,
                    ImporteTotal = d.ImporteTotal,
                    ImportePagado = d.ImportePagado + abonado,
                    Saldo = Math.Max(0m, d.Saldo - abonado),
                    Estado = d.Estado,
                    RequiereRep = d.RequiereRep
                };
            })
            .Where(d => d.Saldo > 0m)
            .ToList();

        var totalAbonado = aplicado.Values.Sum();
        var estado = Estado(idCliente, 0m);

        if (estado is not null)
            estado.SaldoDeudor = Math.Max(0m, estado.SaldoDeudor - totalAbonado);

        return new ResumenCreditoData
        {
            Cliente = new ClienteCreditoResumen
            {
                Id = cliente.Id,
                Codigo = cliente.Codigo,
                Nombre = cliente.Nombre,
                Rfc = cliente.Rfc,
                Estatus = 1
            },
            Credito = estado,
            DocumentosPendientes = documentos
        };
    }
}
