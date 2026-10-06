using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Caja;

public class AsignarCajaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public Caja? Data { get; set; }
}