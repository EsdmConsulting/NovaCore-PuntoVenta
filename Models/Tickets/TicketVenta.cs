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


    // =========================================================
    // PAGO (cuando no se conoce el desglose por forma de pago)
    // =========================================================

    public decimal Pagado { get; set; }


    // =========================================================
    // REIMPRESIÓN
    // =========================================================

    public bool EsReimpresion { get; set; }

    // Administrador que autorizó la reimpresión
    public string ReimpresoPor { get; set; } =
        string.Empty;

    public DateTime? FechaReimpresion { get; set; }


    // =========================================================
    // QR POR DEFECTO
    // =========================================================

    public const string UrlPlataforma =
        "https://plataformaluisfer.com/Plataforma_LuisFer/auth/login";


    // =========================================================
    // TICKET DE MUESTRA (para probar y diseñar el formato)
    // =========================================================

    public static TicketVenta CrearMuestra() =>
        new()
        {
            UnidadOperativa = "SUCURSAL DE PRUEBA",
            FolioTicket = "PV-000-X-00000000",
            Fecha = DateTime.Now,
            Caja = "CAJA-PRUEBA",
            Cajero = "CAJERO DE PRUEBA",

            Productos = new List<TicketVentaDetalle>
            {
                new()
                {
                    Producto = "REFRESCO COLA 600 ML",
                    Presentacion = "Pieza",
                    Cantidad = 2,
                    PrecioUnitario = 25m,
                    Importe = 50m
                },
                new()
                {
                    Producto = "BOTANA SABOR QUESO",
                    Presentacion = "Bolsa 150 g",
                    Cantidad = 1,
                    PrecioUnitario = 30m,
                    Importe = 30m
                },
                new()
                {
                    Producto = "JAMON DE PAVO",
                    Presentacion = "Kilo",
                    Cantidad = 0.5m,
                    PrecioUnitario = 40m,
                    Importe = 20m
                }
            },

            Subtotal = 100m,
            Iva = 16m,
            Total = 116m,

            FormaPago = "Efectivo",
            Efectivo = 116m,
            Recibido = 200m,
            Cambio = 84m,

            ContenidoQr = UrlPlataforma
        };
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