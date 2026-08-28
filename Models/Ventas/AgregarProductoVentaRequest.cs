using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class AgregarProductoVentaRequest
{
    [JsonPropertyName("id_producto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("id_presentacion")]
    public int IdPresentacion { get; set; }

    [JsonPropertyName("cantidad_comercial")]
    public decimal CantidadComercial { get; set; }

    [JsonPropertyName("factor_conversion")]
    public decimal FactorConversion { get; set; } = 1;

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; set; }

    [JsonPropertyName("descuento_porcentaje")]
    public decimal DescuentoPorcentaje { get; set; }

    [JsonPropertyName("descripcion_snapshot")]
    public string DescripcionSnapshot { get; set; } = string.Empty;
}