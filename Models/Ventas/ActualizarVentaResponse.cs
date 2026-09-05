using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class ActualizarVentaRequest
{
    [JsonPropertyName("id_cliente")]
    public int? IdCliente { get; set; }


    [JsonPropertyName("tipo_cliente")]
    [JsonIgnore(
        Condition =
            JsonIgnoreCondition.WhenWritingNull
    )]
    public string? TipoCliente { get; set; }


    [JsonPropertyName("tipo_venta")]
    [JsonIgnore(
        Condition =
            JsonIgnoreCondition.WhenWritingNull
    )]
    public string? TipoVenta { get; set; }
}


public class ActualizarVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public ActualizarVentaData? Data { get; set; }
}


public class ActualizarVentaData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("folio")]
    public string Folio { get; set; } =
        string.Empty;

    [JsonPropertyName("id_cliente")]
    public int? IdCliente { get; set; }

    [JsonPropertyName("tipo_cliente")]
    public string TipoCliente { get; set; } =
        string.Empty;

    [JsonPropertyName("tipo_venta")]
    public string TipoVenta { get; set; } =
        string.Empty;

    [JsonPropertyName("requiere_factura")]
    public bool RequiereFactura { get; set; }

    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } =
        string.Empty;
}