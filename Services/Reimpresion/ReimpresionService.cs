using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Cancelaciones;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Tickets;
using NovaCoreESDM.Services.Cancelaciones;
using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.Services.Printing;

namespace NovaCoreESDM.Services.Reimpresion;

/// <summary>
/// Busca ventas y reimprime sus tickets.
///
/// NO necesita endpoints nuevos en PHP: reutiliza los de cancelaciones
///     GET api/pos/cancelaciones          (lista por fecha)
///     GET api/pos/cancelaciones/{id}     (detalle de una venta)
/// </summary>
public class ReimpresionService
{
    public const int MaximoDiasConsulta = 31;

    private readonly CancelacionesService _cancelaciones = new();
    private readonly TicketVentaService _ticketService = new();
    private readonly TerminalConfigurationService _terminalConfiguracion = new();
    private readonly PrinterConfigurationService _impresoraConfiguracion = new();


    // =========================================================
    // CERRAR LA AUTORIZACIÓN DE ADMINISTRADOR EN EL SERVIDOR
    // =========================================================

    public Task<bool> CerrarAutorizacionAsync() =>
        _cancelaciones.CerrarAutorizacionAsync();


    // =========================================================
    // BUSCAR VENTAS (rango de fechas + folio opcional)
    // =========================================================

    public async Task<(bool Exito, string Mensaje, List<VentaCancelacionItem> Ventas)>
        BuscarAsync(
            DateTime desde,
            DateTime hasta,
            string? folio)
    {
        if (hasta < desde)
            (desde, hasta) = (hasta, desde);

        var dias = (hasta.Date - desde.Date).Days + 1;

        if (dias > MaximoDiasConsulta)
        {
            return (
                false,
                $"El rango máximo de consulta es de {MaximoDiasConsulta} días.",
                new List<VentaCancelacionItem>());
        }

        var texto = folio?.Trim();
        var ventas = new List<VentaCancelacionItem>();

        // El endpoint consulta UN día a la vez: recorremos el rango.
        for (var dia = desde.Date; dia <= hasta.Date; dia = dia.AddDays(1))
        {
            var respuesta =
                await _cancelaciones.ObtenerVentasAsync(dia);

            if (respuesta.Res != 1)
            {
                return (
                    false,
                    respuesta.Msg ??
                    $"No fue posible consultar las ventas del {dia:dd/MM/yyyy}.",
                    new List<VentaCancelacionItem>());
            }

            if (respuesta.Data?.Ventas is not null)
                ventas.AddRange(respuesta.Data.Ventas);
        }

        // Filtro por folio (del ticket o interno) hecho aquí en el equipo.
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var filtradas =
                ventas.Where(v => Coincide(v, texto)).ToList();

            // Respaldo: si no hubo coincidencia, dejamos que el servidor
            // filtre con su propio campo "folio".
            if (filtradas.Count == 0)
            {
                for (var dia = desde.Date; dia <= hasta.Date; dia = dia.AddDays(1))
                {
                    var respuesta =
                        await _cancelaciones.ObtenerVentasAsync(dia, texto);

                    if (respuesta.Res == 1 && respuesta.Data?.Ventas is not null)
                        filtradas.AddRange(respuesta.Data.Ventas);
                }
            }

            ventas = filtradas;
        }

        var ordenadas =
            ventas
                .GroupBy(v => v.Id)
                .Select(g => g.First())
                .OrderByDescending(v => $"{v.Fecha} {v.Hora}")
                .ToList();

        return (true, string.Empty, ordenadas);
    }


    private static bool Coincide(VentaCancelacionItem venta, string texto) =>
        Contiene(venta.FolioTicket, texto) ||
        Contiene(venta.Folio, texto);


    private static bool Contiene(string? valor, string texto) =>
        !string.IsNullOrWhiteSpace(valor) &&
        valor.Contains(texto, StringComparison.OrdinalIgnoreCase);


    // =========================================================
    // REIMPRIMIR
    // =========================================================
    //
    // Lanza excepción si algo falla; el ViewModel muestra el mensaje.
    // =========================================================

    public async Task<string> ReimprimirAsync(
        int idVenta,
        string autorizadoPor)
    {
        var respuesta =
            await _cancelaciones.ObtenerDetalleAsync(idVenta);

        if (respuesta.Res != 1 ||
            respuesta.Data?.Venta is null)
        {
            throw new InvalidOperationException(
                respuesta.Msg ??
                "No fue posible obtener el detalle de la venta.");
        }

        if (string.Equals(
                respuesta.Data.Venta.Estado,
                "CANCELADA",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La venta está cancelada, no se puede reimprimir su ticket.");
        }

        var ticket =
            await ConstruirTicketAsync(
                respuesta.Data,
                autorizadoPor);

        var impreso = false;

        try
        {
            impreso =
                await _ticketService.ImprimirAsync(ticket);
        }
        finally
        {
            RegistrarBitacora(
                ticket.FolioTicket,
                autorizadoPor,
                impreso);
        }

        if (!impreso)
        {
            throw new InvalidOperationException(
                "La impresora no confirmó la impresión del ticket.");
        }

        return $"Ticket {ticket.FolioTicket} enviado a la impresora.";
    }


    // =========================================================
    // ARMAR EL TICKET A PARTIR DEL DETALLE DE LA VENTA
    // =========================================================

    private async Task<TicketVenta> ConstruirTicketAsync(
        CancelacionDetalleData data,
        string autorizadoPor)
    {
        var venta = data.Venta!;

        var terminal =
            await _terminalConfiguracion.ObtenerConfiguracionAsync();

        // Unidad y caja: solo las conocemos por nombre si la venta
        // se hizo en ESTA caja/unidad; si no, mostramos su número.
        var unidad =
            terminal is not null &&
            terminal.IdUnidadOperativa == venta.IdUnidadOperativa
                ? terminal.UnidadCodigo
                : string.Empty;

        var caja =
            terminal is not null &&
            terminal.IdCaja == venta.IdCaja
                ? terminal.CodigoCaja
                : $"Caja {venta.IdCaja}";

        var productos = new List<TicketVentaDetalle>();

        foreach (var linea in data.Detalles)
        {
            // Líneas dadas de baja no deben salir en el ticket.
            if (string.Equals(linea.Estado, "CANCELADO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(linea.Estado, "ELIMINADO", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            productos.Add(
                new TicketVentaDetalle
                {
                    Producto = linea.NombreMostrar,
                    Presentacion = linea.NombrePresentacion ?? string.Empty,
                    Cantidad = linea.CantidadComercial,
                    PrecioUnitario = linea.PrecioUnitario,
                    Importe = linea.TotalLinea
                });
        }

        return new TicketVenta
        {
            UnidadOperativa = unidad,

            FolioTicket =
                !string.IsNullOrWhiteSpace(venta.FolioTicket)
                    ? venta.FolioTicket!
                    : venta.Folio ?? $"VENTA-{venta.Id}",

            Fecha = ParsearFechaHora(venta.Fecha, venta.Hora),

            Caja = caja,
            Cajero = venta.UsuarioRegistro ?? string.Empty,

            Productos = productos,

            Subtotal = venta.Subtotal,
            Iva = venta.TotalIva,
            Ieps = venta.TotalIeps,
            Total = venta.Total,

            // El detalle del servidor no desglosa efectivo/tarjeta/
            // transferencia, por eso se imprime solo el monto pagado.
            Pagado = venta.MontoPagado,

            ContenidoQr = TicketVenta.UrlPlataforma,

            EsReimpresion = true,
            ReimpresoPor = autorizadoPor,
            FechaReimpresion = DateTime.Now
        };
    }


    private static DateTime ParsearFechaHora(string? fecha, string? hora)
    {
        // Algunos servidores mandan la fecha ya con hora incluida.
        var texto =
            !string.IsNullOrWhiteSpace(fecha) && fecha.Contains(':')
                ? fecha
                : $"{fecha} {hora}".Trim();

        return DateTime.TryParse(
            texto,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var resultado)
            ? resultado
            : DateTime.Now;
    }


    // =========================================================
    // BITÁCORA LOCAL DE REIMPRESIONES
    // %LocalAppData%\NovaCoreESDM\reimpresiones.log
    // =========================================================

    private void RegistrarBitacora(
        string folio,
        string autorizadoPor,
        bool impreso)
    {
        try
        {
            Directory.CreateDirectory(
                PrinterConfigurationService.DirectorioDatos);

            var ruta =
                Path.Combine(
                    PrinterConfigurationService.DirectorioDatos,
                    "reimpresiones.log");

            var linea =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}" +
                $" | ticket={folio}" +
                $" | autorizo={autorizadoPor}" +
                $" | sesion={PosSession.NombreUsuario}" +
                $" | caja={PosSession.CodigoCaja}" +
                $" | impresora={_impresoraConfiguracion.ObtenerNombreImpresora()}" +
                $" | impreso={impreso}" +
                Environment.NewLine;

            File.AppendAllText(ruta, linea);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"No fue posible escribir la bitácora de reimpresiones: {ex.Message}");
        }
    }
}
