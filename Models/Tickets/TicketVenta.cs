using System;
using System.Collections.Generic;

namespace NovaCoreESDM.Models.Tickets;

public class TicketVenta
{
    // =========================================================
    // EMPRESA / SUCURSAL
    // =========================================================

    public string Empresa { get; set; } =
        "GRUPO LUISFER";

    public string Sistema { get; set; } =
        "NOVACORE";

    public string UnidadOperativa { get; set; } =
        string.Empty;


    // =========================================================
    // TICKET
    // =========================================================

    public string FolioTicket { get; set; } =
        string.Empty;

    public DateTime Fecha { get; set; } =
        DateTime.Now;

    public string Caja { get; set; } =
        string.Empty;

    public string Cajero { get; set; } =
        string.Empty;


    // =========================================================
    // PRODUCTOS
    // =========================================================

    public List<TicketVentaDetalle> Productos { get; set; } =
        new();


    // =========================================================
    // TOTALES
    // =========================================================

    public decimal Subtotal { get; set; }

    public decimal Iva { get; set; }

    public decimal Ieps { get; set; }

    public decimal Total { get; set; }


    // =========================================================
    // PAGO
    // =========================================================

    public string FormaPago { get; set; } =
        string.Empty;

    public decimal Efectivo { get; set; }

    public decimal Tarjeta { get; set; }

    public decimal Transferencia { get; set; }

    public decimal Recibido { get; set; }

    public decimal Cambio { get; set; }


    // =========================================================
    // QR
    // =========================================================

    public string ContenidoQr { get; set; } =
        string.Empty;
}


public class TicketVentaDetalle
{
    public string Producto { get; set; } =
        string.Empty;

    public string Presentacion { get; set; } =
        string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Importe { get; set; }
}