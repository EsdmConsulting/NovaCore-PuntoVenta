using System.Text.Json.Serialization;

using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class AgregarProductoVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public ProductoVentaAgregado? Data { get; set; }
}

public class ProductoVentaAgregado
{
    [JsonPropertyName("id_detalle")]
    public int IdDetalle { get; set; }

    [JsonPropertyName("id_venta")]
    public int IdVenta { get; set; }

    [JsonPropertyName("id_producto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("id_presentacion")]
    public int IdPresentacion { get; set; }

    [JsonPropertyName("presentacion")]
    public string? Presentacion { get; set; }

    [JsonPropertyName("cantidad_comercial")]
    public decimal CantidadComercial { get; set; }

    [JsonPropertyName("factor_conversion")]
    public decimal FactorConversion { get; set; }

    [JsonPropertyName("cantidad_base")]
    public decimal CantidadBase { get; set; }

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("descuento")]
    public decimal Descuento { get; set; }

    [JsonPropertyName("total_linea")]
    public decimal TotalLinea { get; set; }

    [JsonPropertyName("existencia")]
    public decimal Existencia { get; set; }

    [JsonPropertyName("existencia_apartada")]
    public decimal ExistenciaApartada { get; set; }

    [JsonPropertyName("existencia_disponible")]
    public decimal ExistenciaDisponible { get; set; }
}