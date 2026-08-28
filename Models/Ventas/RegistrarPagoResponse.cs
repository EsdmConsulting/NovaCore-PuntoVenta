using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class RegistrarPagoResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public RegistrarPagoData? Data { get; set; }
}

public class RegistrarPagoData
{
    [JsonPropertyName("id_pago")]
    public int IdPago { get; set; }

    [JsonPropertyName("id_venta")]
    public int IdVenta { get; set; }

    [JsonPropertyName("folio")]
    public string Folio { get; set; }
        = string.Empty;

    [JsonPropertyName("id_forma_pago")]
    public int IdFormaPago { get; set; }

    [JsonPropertyName("forma_pago")]
    public string FormaPago { get; set; }
        = string.Empty;

    [JsonPropertyName("clave_forma_pago")]
    public string ClaveFormaPago { get; set; }
        = string.Empty;

    [JsonPropertyName("importe_aplicado")]
    public decimal ImporteAplicado { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("monto_pagado")]
    public decimal MontoPagado { get; set; }

    [JsonPropertyName("saldo")]
    public decimal Saldo { get; set; }

    [JsonPropertyName("pago_completo")]
    public bool PagoCompleto { get; set; }
}