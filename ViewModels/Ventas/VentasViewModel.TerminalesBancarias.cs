using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Tickets;
using NovaCoreESDM.Models.Ventas;
using NovaCoreESDM.Services.Ventas;

namespace NovaCoreESDM.ViewModels.Ventas;

/// <summary>
/// Cobro con tarjeta ligado a la terminal bancaria del catálogo del ERP:
/// la lista de la caja para elegir, la confirmación cuando una autorización
/// se repite en la misma terminal y el detalle de los vouchers en el ticket.
/// </summary>
public partial class VentasViewModel
{
    private readonly List<TicketPagoTarjeta> _pagosTarjetaTicket = new();

    /// <summary>
    /// La vista pregunta si la autorización repetida es otro voucher
    /// (true = registrar de todos modos).
    /// </summary>
    public Func<string, Task<bool>>? ConfirmarAutorizacionDuplicadaAsync { get; set; }


    /// <summary>Terminales de esta caja (vacía si no hay catálogo: se cobra como antes).</summary>
    public Task<List<TerminalBancariaCaja>> ObtenerTerminalesBancariasAsync() =>
        TerminalesBancariasService.ObtenerAsync(PosSession.IdCaja);


    private TicketPagoTarjeta AgregarPagoTarjetaTicket(
        TerminalBancariaCaja? terminal,
        string? autorizacion,
        string? ultimosDigitos,
        decimal importe,
        bool esDebito,
        string? nombreTerminal = null,
        DetalleTarjetaPago? pagare = null)
    {
        var linea = new TicketPagoTarjeta
        {
            Terminal = terminal?.TextoCorto ?? (nombreTerminal ?? string.Empty).Trim().ToUpperInvariant(),
            Autorizacion = (autorizacion ?? string.Empty).Trim(),
            UltimosDigitos = (ultimosDigitos ?? string.Empty).Trim(),
            EsDebito = esDebito,
            Importe = importe,
            Pagare = pagare
        };

        _pagosTarjetaTicket.Add(linea);
        return linea;
    }


    private void QuitarPagoTarjetaTicket(TicketPagoTarjeta linea) =>
        _pagosTarjetaTicket.Remove(linea);


    /// <summary>Copia para el ticket de la venta que se está terminando.</summary>
    private List<TicketPagoTarjeta> PagosTarjetaParaTicket() => _pagosTarjetaTicket.ToList();
}
