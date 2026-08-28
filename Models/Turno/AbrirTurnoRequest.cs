using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Turno;

public class AbrirTurnoRequest
{
    [JsonPropertyName("id_caja")]
    public int IdCaja { get; set; }

    [JsonPropertyName("fondo_inicial")]
    public decimal FondoInicial { get; set; }

    [JsonPropertyName("observaciones")]
    public string Observaciones { get; set; } = string.Empty;
}