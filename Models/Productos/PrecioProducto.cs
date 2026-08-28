using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Productos;

public class PrecioProducto
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("TipoPrecio")]
    public string TipoPrecio { get; set; } = string.Empty;

    [JsonPropertyName("Precio")]
    public decimal Precio { get; set; }

    [JsonPropertyName("FechaInicio")]
    public string FechaInicio { get; set; } = string.Empty;

    [JsonPropertyName("FechaFin")]
    public string? FechaFin { get; set; }

    [JsonPropertyName("Estatus")]
    public int Estatus { get; set; }
}