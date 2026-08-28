using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class CrearVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public VentaCreada? Data { get; set; }
}

public class VentaCreada
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;
}