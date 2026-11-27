using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class RegistrarPagoRequest
{
    [JsonPropertyName("id_forma_pago")]
    public int IdFormaPago { get; set; }

    [JsonPropertyName("id_banco")]
    public int? IdBanco { get; set; }

    [JsonPropertyName("id_tpv_bancaria")]
    public int? IdTpvBancaria { get; set; }

    [JsonPropertyName("importe")]
    public decimal Importe { get; set; }

    [JsonPropertyName("referencia")]
    public string Referencia { get; set; }
        = string.Empty;

    [JsonPropertyName("autorizacion")]
    public string Autorizacion { get; set; }
        = string.Empty;

    /// <summary>
    /// El cajero confirmó que la autorización repetida en la misma terminal
    /// es otro voucher (409 AUTORIZACION_DUPLICADA). Solo se envía si es true.
    /// </summary>
    [JsonPropertyName("confirmar_autorizacion_duplicada")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ConfirmarAutorizacionDuplicada { get; set; }

    /// <summary>Datos del banco del cobro directo con el lector. Solo se envía si hay.</summary>
    [JsonPropertyName("detalle_tarjeta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DetalleTarjetaPago? DetalleTarjeta { get; set; }
}