using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Turno;

public class CerrarTurnoRequest
{
    [JsonPropertyName("id_turno")]
    public int IdTurno { get; set; }

    [JsonPropertyName("efectivo_declarado")]
    public decimal EfectivoDeclarado { get; set; }

    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }
}