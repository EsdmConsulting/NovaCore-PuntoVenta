using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class FinalizarVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public FinalizarVentaData? Data { get; set; }
}

public class FinalizarVentaData
{
    [JsonPropertyName("id_venta")]
    public int IdVenta { get; set; }

    // Folio técnico:
    // POS-20260820225237556
    [JsonPropertyName("folio")]
    public string Folio { get; set; }
        = string.Empty;

    [JsonPropertyName("serie_ticket")]
    public string SerieTicket { get; set; }
        = string.Empty;

    [JsonPropertyName("numero_ticket")]
    public long NumeroTicket { get; set; }

    // Folio comercial:
    // PV-001-B-00000001
    [JsonPropertyName("folio_ticket")]
    public string FolioTicket { get; set; }
        = string.Empty;

    [JsonPropertyName("codigo_unidad_operativa")]
    public string CodigoUnidadOperativa { get; set; }
        = string.Empty;

    [JsonPropertyName("id_caja")]
    public int IdCaja { get; set; }

    [JsonPropertyName("codigo_caja")]
    public string CodigoCaja { get; set; }
        = string.Empty;

    [JsonPropertyName("nombre_caja")]
    public string NombreCaja { get; set; }
        = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; }
        = string.Empty;

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("monto_pagado")]
    public decimal MontoPagado { get; set; }

    [JsonPropertyName("saldo")]
    public decimal Saldo { get; set; }

    [JsonPropertyName("efectivo")]
    public decimal Efectivo { get; set; }

    [JsonPropertyName("tarjetas")]
    public decimal Tarjetas { get; set; }

    [JsonPropertyName("transferencias")]
    public decimal Transferencias { get; set; }

    [JsonPropertyName("requiere_factura")]
    public bool RequiereFactura { get; set; }

    [JsonPropertyName("inventarios")]
    public List<InventarioFinalizadoData> Inventarios { get; set; }
        = new();
}

public class InventarioFinalizadoData
{
    [JsonPropertyName("id_producto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("id_presentacion")]
    public int IdPresentacion { get; set; }

    [JsonPropertyName("existencia")]
    public decimal Existencia { get; set; }

    [JsonPropertyName("existencia_apartada")]
    public decimal ExistenciaApartada { get; set; }

    [JsonPropertyName("existencia_disponible")]
    public decimal ExistenciaDisponible { get; set; }
}