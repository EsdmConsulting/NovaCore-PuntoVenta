using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using NovaCoreESDM.Models.Configuration;
using NovaCoreESDM.Models.Tickets;
using NovaCoreESDM.Services.Configuration;
using SkiaSharp;

namespace NovaCoreESDM.Services.Printing;

public class TicketVentaService
{
    private readonly WindowsRawPrinterService
        _printerService = new();

    private readonly TicketFormatConfigurationService
        _formatoService = new();


    // =========================================================
    // IMPRIMIR UN TICKET (venta nueva o reimpresión)
    // =========================================================

    public async Task<bool> ImprimirAsync(
        TicketVenta ticket)
    {
        var formato =
            _formatoService.Obtener();

        var contenido =
            ConstruirTicket(ticket, formato);

        var nombreDocumento =
            ticket.EsReimpresion
                ? $"Reimpresion {ticket.FolioTicket}"
                : $"Ticket {ticket.FolioTicket}";

        return await _printerService
            .ImprimirAsync(
                contenido,
                nombreDocumento
            );
    }


    // =========================================================
    // VARIAS COPIAS EN UN SOLO TRABAJO DE IMPRESIÓN
    // =========================================================
    //
    // Cada copia lleva su corte. El pagaré del banco (cobro directo
    // con el lector) sale una sola vez, después de la primera copia.
    // Un solo trabajo evita que la impresora pierda copias cuando se
    // mandan muy seguidas.

    public async Task<bool> ImprimirCopiasAsync(TicketVenta ticket, int copias)
    {
        copias = Math.Clamp(copias, 1, 10);

        var formato = _formatoService.Obtener();
        var contenido = new List<byte>();

        for (var i = 0; i < copias; i++)
            contenido.AddRange(ConstruirTicket(ticket, formato, incluirPagare: i == 0));

        return await _printerService.ImprimirAsync(
            contenido.ToArray(),
            copias == 1 ? $"Ticket {ticket.FolioTicket}" : $"Ticket {ticket.FolioTicket} ({copias} copias)");
    }


    /// <summary>
    /// Copias según cómo se pagó (pedido de la empresa):
    /// efectivo 2, tarjeta 3, transferencia o crédito 5.
    /// Pago mixto: las de la forma que pida más.
    /// </summary>
    public static int CopiasSegunFormaPago(TicketVenta ticket)
    {
        var esCredito =
            (ticket.FormaPago ?? string.Empty).Contains("dito", StringComparison.OrdinalIgnoreCase) &&
            (ticket.FormaPago ?? string.Empty).StartsWith("Cr", StringComparison.OrdinalIgnoreCase);

        if (esCredito || ticket.Transferencia > 0)
            return 5;

        if (ticket.Tarjeta > 0)
            return 3;

        return 2;
    }


    // =========================================================
    // TICKET DE MUESTRA (para probar el formato sin hacer una venta)
    // =========================================================

    public Task<bool> ImprimirMuestraAsync() =>
        ImprimirAsync(TicketVenta.CrearMuestra());


    // =========================================================
    // PRE-CORTE PROVISIONAL (corte hecho sin internet)
    // =========================================================

    public sealed class DatosPreCorte
    {
        public string Caja { get; set; } = string.Empty;
        public string Cajero { get; set; } = string.Empty;
        public int IdTurno { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal EfectivoContado { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public int VentasSinConexion { get; set; }
        public int VentasCanceladas { get; set; }
        public decimal Efectivo { get; set; }
        public decimal Tarjeta { get; set; }
        public decimal Credito { get; set; }
        public int Abonos { get; set; }
        public decimal AbonosTotal { get; set; }
    }


    /// <summary>
    /// Comprobante del corte hecho sin internet. No es el corte oficial: el
    /// servidor lo cierra (y calcula esperado y diferencia) al volver la conexión.
    /// </summary>
    public async Task<bool> ImprimirPreCorteAsync(DatosPreCorte d)
    {
        var formato = _formatoService.Obtener();
        var ancho = formato.AnchoCaracteres;
        var bytes = new List<byte>();

        bytes.AddRange(new byte[] { 0x1B, 0x40 });

        Centrar(bytes);
        Negrita(bytes, true);
        foreach (var linea in formato.Encabezado)
            AgregarTexto(bytes, linea + "\n");
        AgregarTexto(bytes, "\nPRE-CORTE (SIN CONEXION)\n");
        Negrita(bytes, false);
        AgregarTexto(bytes, "Provisional - no es el corte oficial\n\n");

        Izquierda(bytes);
        AgregarTexto(bytes, AlinearDosColumnas("Caja:", d.Caja, ancho));
        AgregarTexto(bytes, AlinearDosColumnas("Cajero:", d.Cajero, ancho));
        AgregarTexto(bytes, AlinearDosColumnas("Turno:", d.IdTurno.ToString(), ancho));
        AgregarTexto(bytes, AlinearDosColumnas("Fecha:", d.Fecha.ToString("dd/MM/yyyy HH:mm"), ancho));
        SeparadorDoble(bytes, ancho);

        Negrita(bytes, true);
        AgregarTexto(bytes, AlinearDosColumnas("EFECTIVO CONTADO:", $"${d.EfectivoContado:N2}", ancho));
        Negrita(bytes, false);
        Separador(bytes, ancho);

        AgregarTexto(bytes, "Operaciones sin conexion del turno\n");
        AgregarTexto(bytes, AlinearDosColumnas("Ventas:", d.VentasSinConexion.ToString(), ancho));
        if (d.VentasCanceladas > 0)
            AgregarTexto(bytes, AlinearDosColumnas("Canceladas:", d.VentasCanceladas.ToString(), ancho));
        AgregarTexto(bytes, AlinearDosColumnas("Efectivo:", $"${d.Efectivo:N2}", ancho));
        AgregarTexto(bytes, AlinearDosColumnas("Tarjeta:", $"${d.Tarjeta:N2}", ancho));
        AgregarTexto(bytes, AlinearDosColumnas("Credito:", $"${d.Credito:N2}", ancho));
        AgregarTexto(bytes, AlinearDosColumnas($"Abonos ({d.Abonos}):", $"${d.AbonosTotal:N2}", ancho));
        Separador(bytes, ancho);

        if (!string.IsNullOrWhiteSpace(d.Observaciones))
            AgregarTexto(bytes, $"Obs: {d.Observaciones}\n");

        AgregarTexto(bytes,
            "\nEl corte oficial (efectivo esperado y\ndiferencia) se cierra en el servidor\n" +
            "cuando regrese el internet. Se puede\nreimprimir desde Cortes.\n");

        AgregarTexto(bytes, "\n\n\n_____________________________\n");
        Centrar(bytes);
        AgregarTexto(bytes, "Firma del cajero\n");

        AgregarTexto(bytes, new string('\n', Math.Max(0, formato.LineasEnBlancoAlFinal)));

        if (formato.CortarPapel)
            bytes.AddRange(new byte[] { 0x1D, 0x56, 0x01 });

        return await _printerService.ImprimirAsync(bytes.ToArray(), $"Pre-corte turno {d.IdTurno}");
    }


    // =========================================================
    // COMPROBANTE DE DEVOLUCIÓN (CHG-0006)
    // =========================================================

    public sealed class DatosDevolucion
    {
        public string FolioDevolucion { get; set; } = string.Empty;
        public string FolioTicket { get; set; } = string.Empty;
        public string FechaVenta { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Caja { get; set; } = string.Empty;
        public string Cajero { get; set; } = string.Empty;
        public string Autorizo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public decimal Importe { get; set; }
        public List<TicketVentaDetalle> Productos { get; set; } = new();
    }


    public async Task<bool> ImprimirDevolucionAsync(DatosDevolucion d)
    {
        var formato = _formatoService.Obtener();
        var ancho = formato.AnchoCaracteres;
        var bytes = new List<byte>();

        bytes.AddRange(new byte[] { 0x1B, 0x40 });

        Centrar(bytes);
        Negrita(bytes, true);
        foreach (var linea in formato.Encabezado)
            AgregarTexto(bytes, linea + "\n");
        AgregarTexto(bytes, "\nCOMPROBANTE DE DEVOLUCION\n");
        Negrita(bytes, false);
        AgregarTexto(bytes, $"{d.FolioDevolucion}\n\n");

        Izquierda(bytes);
        AgregarTexto(bytes, AlinearDosColumnas("Ticket:", d.FolioTicket, ancho));
        if (!string.IsNullOrWhiteSpace(d.FechaVenta))
            AgregarTexto(bytes, AlinearDosColumnas("Fecha de venta:", d.FechaVenta, ancho));
        AgregarTexto(bytes, AlinearDosColumnas("Fecha:", d.Fecha.ToString("dd/MM/yyyy HH:mm"), ancho));
        AgregarTexto(bytes, AlinearDosColumnas("Caja:", d.Caja, ancho));
        AgregarTexto(bytes, AlinearDosColumnas("Cajero:", d.Cajero, ancho));
        Separador(bytes, ancho);

        Negrita(bytes, true);
        AgregarTexto(bytes, "PRODUCTOS DEVUELTOS\n");
        Negrita(bytes, false);
        AgregarTexto(bytes, "\n");

        foreach (var producto in d.Productos)
        {
            AgregarTexto(bytes, $"{producto.Producto}\n");

            if (!string.IsNullOrWhiteSpace(producto.Presentacion))
                AgregarTexto(bytes, $"{producto.Presentacion}\n");

            var cantidad = producto.Cantidad.ToString("0.##", CultureInfo.InvariantCulture);

            AgregarTexto(
                bytes,
                AlinearDosColumnas(
                    $"{cantidad} x ${producto.PrecioUnitario:N2}",
                    $"${producto.Importe:N2}",
                    ancho));

            AgregarTexto(bytes, "\n");
        }

        SeparadorDoble(bytes, ancho);
        Negrita(bytes, true);
        AgregarTexto(bytes, AlinearDosColumnas("EFECTIVO DEVUELTO:", $"${d.Importe:N2}", ancho));
        Negrita(bytes, false);
        Separador(bytes, ancho);

        if (!string.IsNullOrWhiteSpace(d.Motivo))
            AgregarTexto(bytes, $"Motivo: {d.Motivo}\n");

        if (!string.IsNullOrWhiteSpace(d.Autorizo))
            AgregarTexto(bytes, $"Autorizo: {d.Autorizo}\n");

        AgregarTexto(bytes, "\n\n\n_____________________________\n");
        Centrar(bytes);
        AgregarTexto(bytes, "Firma del cliente\n");

        AgregarTexto(bytes, new string('\n', Math.Max(0, formato.LineasEnBlancoAlFinal)));

        if (formato.CortarPapel)
            bytes.AddRange(new byte[] { 0x1D, 0x56, 0x01 });

        return await _printerService.ImprimirAsync(bytes.ToArray(), $"Devolucion {d.FolioDevolucion}");
    }


    // =========================================================
    // CONSTRUIR EL TICKET
    // =========================================================

    private static byte[] ConstruirTicket(
        TicketVenta ticket,
        TicketFormatConfiguration formato,
        bool incluirPagare = true)
    {
        var bytes = new List<byte>();
        var ancho = formato.AnchoCaracteres;

        // Inicializar impresora
        bytes.AddRange(new byte[] { 0x1B, 0x40 });


        // =====================================================
        // LOGO
        // =====================================================

        if (formato.MostrarLogo)
        {
            Centrar(bytes);
            AgregarLogo(bytes);
            AgregarTexto(bytes, "\n");
        }


        // =====================================================
        // ENCABEZADO
        // =====================================================

        Centrar(bytes);
        Negrita(bytes, true);

        foreach (var linea in formato.Encabezado)
            AgregarTexto(bytes, linea + "\n");

        Negrita(bytes, false);

        if (formato.MostrarUnidadOperativa &&
            !string.IsNullOrWhiteSpace(ticket.UnidadOperativa))
        {
            AgregarTexto(bytes, ticket.UnidadOperativa + "\n");
        }

        AgregarTexto(bytes, "\n");


        // =====================================================
        // AVISO DE REIMPRESIÓN
        // =====================================================

        if (ticket.EsReimpresion)
        {
            Negrita(bytes, true);
            AgregarTexto(bytes, "*** REIMPRESION ***\n");
            Negrita(bytes, false);

            var fechaReimpresion =
                ticket.FechaReimpresion ?? DateTime.Now;

            AgregarTexto(
                bytes,
                $"Reimpreso: {fechaReimpresion:dd/MM/yyyy HH:mm}\n");

            if (!string.IsNullOrWhiteSpace(ticket.ReimpresoPor))
                AgregarTexto(bytes, $"Autorizo: {ticket.ReimpresoPor}\n");

            AgregarTexto(bytes, "\n");
        }


        // =====================================================
        // DATOS DE LA VENTA
        // =====================================================

        Izquierda(bytes);
        Separador(bytes, ancho);

        AgregarTexto(bytes, $"Ticket: {ticket.FolioTicket}\n");
        AgregarTexto(bytes, $"Fecha: {ticket.Fecha:dd/MM/yyyy HH:mm}\n");
        AgregarTexto(bytes, $"Caja: {ticket.Caja}\n");

        if (formato.MostrarCajero)
            AgregarTexto(bytes, $"Cajero: {ticket.Cajero}\n");

        Separador(bytes, ancho);


        // =====================================================
        // PRODUCTOS
        // =====================================================

        Negrita(bytes, true);
        AgregarTexto(bytes, "PRODUCTOS\n");
        Negrita(bytes, false);
        AgregarTexto(bytes, "\n");

        foreach (var producto in ticket.Productos)
        {
            AgregarTexto(bytes, $"{producto.Producto}\n");

            if (!string.IsNullOrWhiteSpace(producto.Presentacion))
                AgregarTexto(bytes, $"{producto.Presentacion}\n");

            var cantidad =
                producto.Cantidad.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture);

            var izquierda =
                $"{cantidad} x ${producto.PrecioUnitario:N2}";

            AgregarTexto(
                bytes,
                AlinearDosColumnas(
                    izquierda,
                    $"${producto.Importe:N2}",
                    ancho));

            AgregarTexto(bytes, "\n");
        }


        // =====================================================
        // PRODUCTOS CANCELADOS CON AUTORIZACIÓN (CHG-0002)
        // =====================================================

        if (ticket.ProductosCancelados.Count > 0)
        {
            Separador(bytes, ancho);

            Negrita(bytes, true);
            AgregarTexto(bytes, "PRODUCTOS CANCELADOS\n");
            Negrita(bytes, false);
            AgregarTexto(bytes, "\n");

            foreach (var cancelado in ticket.ProductosCancelados)
            {
                AgregarTexto(bytes, $"{cancelado.Producto}\n");

                if (!string.IsNullOrWhiteSpace(cancelado.Presentacion))
                    AgregarTexto(bytes, $"{cancelado.Presentacion}\n");

                var cantidad =
                    cancelado.Cantidad.ToString(
                        "0.##",
                        CultureInfo.InvariantCulture);

                AgregarTexto(
                    bytes,
                    AlinearDosColumnas(
                        $"-{cantidad} x ${cancelado.PrecioUnitario:N2}",
                        $"-${cancelado.Importe:N2}",
                        ancho));

                if (!string.IsNullOrWhiteSpace(cancelado.Autorizo))
                    AgregarTexto(bytes, $"Autorizo: {cancelado.Autorizo}\n");

                AgregarTexto(bytes, "\n");
            }
        }


        // =====================================================
        // TOTALES
        // =====================================================

        Separador(bytes, ancho);

        AgregarTexto(
            bytes,
            AlinearDosColumnas("SUBTOTAL:", $"${ticket.Subtotal:N2}", ancho));

        if (formato.MostrarImpuestos)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("IVA:", $"${ticket.Iva:N2}", ancho));

            if (ticket.Ieps > 0)
            {
                AgregarTexto(
                    bytes,
                    AlinearDosColumnas("IEPS:", $"${ticket.Ieps:N2}", ancho));
            }
        }

        Negrita(bytes, true);

        AgregarTexto(
            bytes,
            AlinearDosColumnas("TOTAL:", $"${ticket.Total:N2}", ancho));

        Negrita(bytes, false);

        SeparadorDoble(bytes, ancho);


        // =====================================================
        // FORMA DE PAGO
        // =====================================================

        Negrita(bytes, true);
        AgregarTexto(bytes, "FORMA DE PAGO\n");
        Negrita(bytes, false);

        var hayDesglose =
            ticket.Efectivo > 0 ||
            ticket.Tarjeta > 0 ||
            ticket.Transferencia > 0;

        if (ticket.Efectivo > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Efectivo:", $"${ticket.Efectivo:N2}", ancho));
        }

        if (ticket.Tarjeta > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Tarjeta:", $"${ticket.Tarjeta:N2}", ancho));

            // Detalle de cada voucher: terminal, autorización y tarjeta.
            foreach (var pago in ticket.PagosTarjeta)
            {
                var tipo = pago.EsDebito ? "Debito" : "Credito";
                var cabecera = string.IsNullOrWhiteSpace(pago.Terminal) ? tipo : $"{pago.Terminal} {tipo}";

                AgregarTexto(bytes, AlinearDosColumnas($" {cabecera}", $"${pago.Importe:N2}", ancho));

                var detalle = string.IsNullOrWhiteSpace(pago.Autorizacion) ? string.Empty : $" Aut. {pago.Autorizacion}";

                if (!string.IsNullOrWhiteSpace(pago.UltimosDigitos))
                    detalle += $"  Tarj. ****{pago.UltimosDigitos}";

                if (detalle.Length > 0)
                    AgregarTexto(bytes, detalle + "\n");
            }
        }

        if (ticket.Transferencia > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Transferencia:", $"${ticket.Transferencia:N2}", ancho));
        }

        // Reimpresiones: si el servidor no entrega el desglose,
        // se imprime solo el monto pagado.
        if (!hayDesglose && ticket.Pagado > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Pagado:", $"${ticket.Pagado:N2}", ancho));
        }

        if (ticket.Recibido > 0 && ticket.Efectivo > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Recibido:", $"${ticket.Recibido:N2}", ancho));

            AgregarTexto(
                bytes,
                AlinearDosColumnas("Cambio:", $"${ticket.Cambio:N2}", ancho));
        }


        // =====================================================
        // PIE
        // =====================================================

        Separador(bytes, ancho);
        Centrar(bytes);
        AgregarTexto(bytes, "\n");

        if (!string.IsNullOrWhiteSpace(formato.MensajeFinal))
        {
            Negrita(bytes, true);
            AgregarTexto(bytes, formato.MensajeFinal + "\n");
            Negrita(bytes, false);
            AgregarTexto(bytes, "\n");
        }

        foreach (var linea in formato.Pie)
            AgregarTexto(bytes, linea + "\n");

        AgregarTexto(bytes, $"Ticket:\n{ticket.FolioTicket}\n\n");


        // =====================================================
        // QR
        // =====================================================

        if (formato.MostrarQr &&
            !string.IsNullOrWhiteSpace(ticket.ContenidoQr))
        {
            if (!string.IsNullOrWhiteSpace(formato.TextoQr))
                AgregarTexto(bytes, formato.TextoQr + "\n\n");

            AgregarQr(bytes, ticket.ContenidoQr);


           /*  if (!string.IsNullOrWhiteSpace(formato.TextoAviso))
                AgregarTexto(bytes, formato.TextoAviso + "\n\n"); */

            AgregarTexto(bytes, "\n\n");
        }


        // =====================================================
        // UUID DE LA VENTA
        // =====================================================

        if (!string.IsNullOrWhiteSpace(ticket.Uuid))
        {
            Centrar(bytes);
            AgregarTexto(bytes, $"FOLIO:\n{ticket.Uuid}\n\n");
        }


        AgregarTexto(
            bytes,
            new string('\n', Math.Max(0, formato.LineasEnBlancoAlFinal)));


        // Corte parcial
        if (formato.CortarPapel)
            bytes.AddRange(new byte[] { 0x1D, 0x56, 0x01 });


        // =====================================================
        // PAGARÉ DEL BANCO (cobro directo con el lector)
        // =====================================================
        //
        // BBVA exige dos copias: la del comercio (con la firma
        // cuando la tarjeta la pide) y la del cliente.

        foreach (var pago in ticket.PagosTarjeta)
        {
            if (!incluirPagare || pago.Pagare is null)
                continue;

            AgregarPagare(bytes, ticket, pago, formato, "COMERCIO");
            AgregarPagare(bytes, ticket, pago, formato, "CLIENTE");
        }


        return bytes.ToArray();
    }


    // =========================================================
    // PAGARÉ (formato de "Ejemplos de Tickets" de TOTAL POS SDK)
    // =========================================================

    public const string LeyendaPagare =
        "Por este pagare me obligo incondicionalmente a pagar a la orden del banco " +
        "acreditante el importe de este titulo. Este pagare procede del contrato de " +
        "apertura de credito que el banco acreditante y el tarjetahabiente tienen celebrado.";

    private static void AgregarPagare(
        List<byte> bytes,
        TicketVenta ticket,
        TicketPagoTarjeta pago,
        TicketFormatConfiguration formato,
        string copia)
    {
        var d = pago.Pagare!;
        var ancho = formato.AnchoCaracteres;

        bytes.AddRange(new byte[] { 0x1B, 0x40 });

        Centrar(bytes);

        if (d.EsSimulador)
        {
            Negrita(bytes, true);
            AgregarTexto(bytes, "*** PRUEBA - SIN VALOR ***\n");
            Negrita(bytes, false);
        }

        Negrita(bytes, true);
        AgregarTexto(bytes, "BBVA\n");

        // Nombre (primera línea del encabezado) y domicilio del comercio.
        var encabezado = formato.Encabezado.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        AgregarTexto(bytes, (encabezado.FirstOrDefault() ?? ticket.Empresa) + "\n");
        Negrita(bytes, false);

        foreach (var linea in encabezado.Skip(1))
            AgregarTexto(bytes, linea + "\n");

        if (!string.IsNullOrWhiteSpace(ticket.UnidadOperativa))
            AgregarTexto(bytes, ticket.UnidadOperativa + "\n");

        if (ticket.EsReimpresion)
        {
            Negrita(bytes, true);
            AgregarTexto(bytes, "*** REIMPRESION ***\n");
            Negrita(bytes, false);
        }

        AgregarTexto(bytes, "\n");

        Izquierda(bytes);

        if (!string.IsNullOrWhiteSpace(d.Afiliacion))
            AgregarTexto(bytes, $"AFILIACION: {d.Afiliacion}\n");

        Negrita(bytes, true);
        AgregarTexto(bytes, (string.IsNullOrWhiteSpace(d.NombreTransaccion) ? (d.Operacion ?? "VENTA") : d.NombreTransaccion).ToUpperInvariant() + "\n");
        Negrita(bytes, false);

        if (!string.IsNullOrWhiteSpace(d.Ultimos4))
            AgregarTexto(bytes, $"TARJETA: ************{d.Ultimos4}\n");

        var marca = string.Join(" ", new[]
        {
            d.Marca,
            d.Producto == "DEBITO" ? "DEBITO" : d.Producto == "CREDITO" ? "CREDITO" : null
        }.Where(t => !string.IsNullOrWhiteSpace(t)));

        if (marca.Length > 0)
            AgregarTexto(bytes, marca.ToUpperInvariant() + "\n");

        if (!string.IsNullOrWhiteSpace(d.Emisor))
            AgregarTexto(bytes, d.Emisor.ToUpperInvariant() + "\n");

        Negrita(bytes, true);
        AgregarTexto(bytes, $"APROBADA: {d.Autorizacion ?? pago.Autorizacion}\n");
        Negrita(bytes, false);

        if (d.FormatoTransaccion.Length > 0)
            AgregarTexto(bytes, d.FormatoTransaccion + "\n");

        if (!string.IsNullOrWhiteSpace(d.Arqc))
            AgregarTexto(bytes, $"ARQC: {d.Arqc}\n");

        if (!string.IsNullOrWhiteSpace(d.Aid))
            AgregarTexto(bytes, $"AID: {d.Aid}\n");

        if (!string.IsNullOrWhiteSpace(d.Aplicacion))
            AgregarTexto(bytes, $"{d.Aplicacion}\n");

        AgregarTexto(bytes, "\n");
        Negrita(bytes, true);
        AgregarTexto(bytes, AlinearDosColumnas("IMPORTE:", $"${pago.Importe:N2} M.N.", ancho));
        Negrita(bytes, false);
        AgregarTexto(bytes, "\n");

        var operador = string.Join(" ", new[] { ticket.Caja, ticket.Cajero }.Where(t => !string.IsNullOrWhiteSpace(t)));

        if (operador.Length > 0)
            AgregarTexto(bytes, $"OPER: {operador}\n");

        if (!string.IsNullOrWhiteSpace(d.ReferenciaFinanciera))
            AgregarTexto(bytes, $"REF: {d.ReferenciaFinanciera}\n");

        if (!string.IsNullOrWhiteSpace(d.NumeroTerminal))
            AgregarTexto(bytes, $"CAJA: {d.NumeroTerminal}\n");

        if (!string.IsNullOrWhiteSpace(d.Secuencia))
            AgregarTexto(bytes, $"SEC TNX: {d.Secuencia}\n");

        AgregarTexto(bytes, $"TICKET: {ticket.FolioTicket}\n");
        AgregarTexto(bytes, "\n");

        foreach (var linea in Envolver(LeyendaPagare, ancho))
            AgregarTexto(bytes, linea + "\n");

        AgregarTexto(bytes, "\n");
        Centrar(bytes);

        // Firma: la decide el chip de la tarjeta (la informa el banco).
        switch (d.Firma)
        {
            case "ELECTRONICA":
                foreach (var linea in Envolver("AUTORIZADO MEDIANTE FIRMA ELECTRONICA", ancho))
                    AgregarTexto(bytes, linea + "\n");
                break;

            case "SIN_FIRMA":
                foreach (var linea in Envolver("AUTORIZADO SIN FIRMA", ancho))
                    AgregarTexto(bytes, linea + "\n");
                break;

            default:
                // Autógrafa (o sin dato): espacio para firmar en la copia del comercio.
                if (copia == "COMERCIO")
                {
                    AgregarTexto(bytes, "\n\n\n");
                    AgregarTexto(bytes, new string('_', Math.Min(32, ancho)) + "\n");
                    AgregarTexto(bytes, "FIRMA\n");
                }
                break;
        }

        if (!string.IsNullOrWhiteSpace(d.Tarjetahabiente))
            AgregarTexto(bytes, d.Tarjetahabiente.ToUpperInvariant() + "\n");

        var fecha = ticket.EsReimpresion || string.IsNullOrWhiteSpace(d.FechaBanco)
            ? ticket.Fecha.ToString("dd/MM/yyyy HH:mm:ss")
            : d.FechaBanco;

        AgregarTexto(bytes, fecha + "\n\n");

        foreach (var linea in Envolver("PAGARE NEGOCIABLE UNICAMENTE EN INSTITUCIONES DE CREDITO", ancho))
            AgregarTexto(bytes, linea + "\n");

        AgregarTexto(bytes, "\n");
        Negrita(bytes, true);
        AgregarTexto(bytes, $"- {copia} -\n");
        Negrita(bytes, false);

        AgregarTexto(bytes, new string('\n', Math.Max(0, formato.LineasEnBlancoAlFinal)));

        if (formato.CortarPapel)
            bytes.AddRange(new byte[] { 0x1D, 0x56, 0x01 });
    }


    /// <summary>Parte un texto largo en renglones del ancho del papel (por palabras).</summary>
    public static List<string> Envolver(string texto, int ancho)
    {
        var lineas = new List<string>();
        var actual = new StringBuilder();

        foreach (var palabra in (texto ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (actual.Length > 0 && actual.Length + 1 + palabra.Length > ancho)
            {
                lineas.Add(actual.ToString());
                actual.Clear();
            }

            if (actual.Length > 0)
                actual.Append(' ');

            actual.Append(palabra);
        }

        if (actual.Length > 0)
            lineas.Add(actual.ToString());

        return lineas;
    }


    // =========================================================
    // HELPERS
    // =========================================================

    /// <summary>
    /// La impresora recibe ASCII. Quitamos los acentos (á -> a)
    /// para que no salgan signos "?" en el ticket.
    /// </summary>
    private static string Sanitizar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        var descompuesto =
            texto.Normalize(NormalizationForm.FormD);

        var sb =
            new StringBuilder(descompuesto.Length);

        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c)
                == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }


    private static void AgregarTexto(
        List<byte> bytes,
        string texto)
    {
        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                Sanitizar(texto)));
    }


    private static void Centrar(List<byte> bytes)
    {
        bytes.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
    }


    private static void Izquierda(List<byte> bytes)
    {
        bytes.AddRange(new byte[] { 0x1B, 0x61, 0x00 });
    }


    private static void Negrita(List<byte> bytes, bool activa)
    {
        bytes.AddRange(
            new byte[]
            {
                0x1B,
                0x45,
                activa ? (byte)0x01 : (byte)0x00
            });
    }


    private static void Separador(List<byte> bytes, int ancho)
    {
        AgregarTexto(bytes, new string('-', ancho) + "\n");
    }


    private static void SeparadorDoble(List<byte> bytes, int ancho)
    {
        AgregarTexto(bytes, new string('=', ancho) + "\n");
    }


    private static string AlinearDosColumnas(
        string izquierda,
        string derecha,
        int ancho)
    {
        izquierda = Sanitizar(izquierda);
        derecha = Sanitizar(derecha);

        var espacios =
            ancho - izquierda.Length - derecha.Length;

        if (espacios < 1)
        {
            // Si no cabe, recortamos el texto de la izquierda.
            var maximo =
                Math.Max(0, ancho - derecha.Length - 1);

            if (izquierda.Length > maximo)
                izquierda = izquierda.Substring(0, maximo);

            espacios = 1;
        }

        return izquierda
               + new string(' ', espacios)
               + derecha
               + "\n";
    }


    private static void AgregarQr(
        List<byte> bytes,
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return;

        var datos =
            Encoding.UTF8.GetBytes(contenido);

        // =====================================================
        // MODELO QR
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                0x04, 0x00,
                0x31, 0x41,
                0x32, 0x00
            });


        // =====================================================
        // TAMAÑO QR
        // 1 - 16
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                0x03, 0x00,
                0x31, 0x43,
                0x06
            });


        // =====================================================
        // CORRECCIÓN DE ERROR
        // 0x31 = Nivel M
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                0x03, 0x00,
                0x31, 0x45,
                0x31
            });


        // =====================================================
        // GUARDAR INFORMACIÓN DEL QR
        // =====================================================

        var longitud =
            datos.Length + 3;

        var pL =
            (byte)(longitud & 0xFF);

        var pH =
            (byte)((longitud >> 8) & 0xFF);


        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                pL, pH,
                0x31, 0x50, 0x30
            });


        bytes.AddRange(
            datos);


        // =====================================================
        // IMPRIMIR QR
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                0x03, 0x00,
                0x31, 0x51, 0x30
            });
    }
    
    
    
    // =========================================================
// LOGO GRUPO LUISFER
// =========================================================

private static void AgregarLogo(
    List<byte> bytes)
{
    try
    {
        var uri =
            new Uri(
                "avares://PuntoDeVenta/Assets/logo_grupo_luisfer.png"
            );

        using var stream =
            AssetLoader.Open(uri);

        using var original =
            SKBitmap.Decode(stream);

        if (original is null)
        {
            Console.WriteLine(
                "No fue posible decodificar el logo."
            );

            return;
        }


        // =====================================================
        // REDIMENSIONAR
        // =====================================================

        const int anchoLogo =
            360;

        var proporcion =
            (double)anchoLogo /
            original.Width;

        var altoLogo =
            Math.Max(
                1,
                (int)Math.Round(
                    original.Height *
                    proporcion
                )
            );


        var info =
            new SKImageInfo(
                anchoLogo,
                altoLogo,
                SKColorType.Rgba8888,
                SKAlphaType.Premul
            );


        using var redimensionada =
            original.Resize(
                info,
                SKFilterQuality.High
            );


        if (redimensionada is null)
        {
            Console.WriteLine(
                "No fue posible redimensionar el logo."
            );

            return;
        }


        // =====================================================
        // CONVERTIR A RASTER ESC/POS
        // =====================================================

        var ancho =
            redimensionada.Width;

        var alto =
            redimensionada.Height;

        var bytesPorFila =
            (ancho + 7) / 8;


        // GS v 0
        bytes.AddRange(
            new byte[]
            {
                0x1D,
                0x76,
                0x30,
                0x00
            }
        );


        // Ancho en bytes
        bytes.Add(
            (byte)(
                bytesPorFila &
                0xFF
            )
        );

        bytes.Add(
            (byte)(
                (
                    bytesPorFila >>
                    8
                ) &
                0xFF
            )
        );


        // Alto en puntos
        bytes.Add(
            (byte)(
                alto &
                0xFF
            )
        );

        bytes.Add(
            (byte)(
                (
                    alto >>
                    8
                ) &
                0xFF
            )
        );


        // =====================================================
        // PIXELES
        // =====================================================

        for (
            var y = 0;
            y < alto;
            y++
        )
        {
            for (
                var byteX = 0;
                byteX < bytesPorFila;
                byteX++
            )
            {
                byte valor =
                    0;


                for (
                    var bit = 0;
                    bit < 8;
                    bit++
                )
                {
                    var x =
                        (byteX * 8)
                        + bit;


                    if (x >= ancho)
                        continue;


                    var pixel =
                        redimensionada.GetPixel(
                            x,
                            y
                        );


                    // Transparencia = blanco
                    if (pixel.Alpha < 20)
                        continue;


                    // Convertir RGB a luminosidad
                    var luminosidad =
                        (
                            pixel.Red * 0.299
                            +
                            pixel.Green * 0.587
                            +
                            pixel.Blue * 0.114
                        );


                    /*
                     * Threshold:
                     *
                     * menor = negro
                     * mayor = blanco
                     *
                     * 210 conserva bastante bien
                     * amarillos/verdes del logo.
                     */
                    var esNegro =
                        luminosidad < 210;


                    if (esNegro)
                    {
                        valor |=
                            (byte)(
                                0x80 >>
                                bit
                            );
                    }
                }


                bytes.Add(
                    valor
                );
            }
        }


        AgregarTexto(
            bytes,
            "\n"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"No fue posible cargar el logo del ticket: {ex.Message}"
        );
    }
}


}