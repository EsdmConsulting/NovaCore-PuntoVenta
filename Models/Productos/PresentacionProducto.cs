using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Productos;

public class PresentacionProducto
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("NombrePresentacion")]
    public string NombrePresentacion { get; set; } = string.Empty;

    [JsonPropertyName("UnidadMedida")]
    public string UnidadMedida { get; set; } = string.Empty;

    [JsonPropertyName("FactorConversion")]
    public decimal FactorConversion { get; set; }

    [JsonPropertyName("CodigoBarras")]
    public string CodigoBarras { get; set; } = string.Empty;

    [JsonPropertyName("EsBase")]
    public bool EsBase { get; set; }

    [JsonPropertyName("Estatus")]
    public int Estatus { get; set; }

    [JsonPropertyName("Precios")]
    public List<PrecioProducto> Precios { get; set; } = new();
}