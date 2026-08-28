using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Productos;

public class DisponibilidadProducto
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("UnidadCodigo")]
    public string UnidadCodigo { get; set; } = string.Empty;

    [JsonPropertyName("UnidadNombre")]
    public string UnidadNombre { get; set; } = string.Empty;

    [JsonPropertyName("TipoClave")]
    public string TipoClave { get; set; } = string.Empty;

    [JsonPropertyName("TipoNombre")]
    public string TipoNombre { get; set; } = string.Empty;

    [JsonPropertyName("StockMinimo")]
    public decimal StockMinimo { get; set; }

    [JsonPropertyName("StockMaximo")]
    public decimal StockMaximo { get; set; }

    [JsonPropertyName("PuntoReorden")]
    public decimal PuntoReorden { get; set; }

    [JsonPropertyName("CostoPromedio")]
    public decimal CostoPromedio { get; set; }

    [JsonPropertyName("PrecioMenudeo")]
    public decimal? PrecioMenudeo { get; set; }

    [JsonPropertyName("PrecioMayoreo")]
    public decimal? PrecioMayoreo { get; set; }

    [JsonPropertyName("UbicacionFisica")]
    public string UbicacionFisica { get; set; } = string.Empty;

    [JsonPropertyName("Estatus")]
    public int Estatus { get; set; }
}