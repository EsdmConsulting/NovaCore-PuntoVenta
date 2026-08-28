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
}