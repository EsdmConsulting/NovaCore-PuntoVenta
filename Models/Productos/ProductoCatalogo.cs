using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Productos;

public class ProductoCatalogo
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Codigo")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("Sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("CodigoBarras")]
    public string CodigoBarras { get; set; } = string.Empty;

    [JsonPropertyName("NombreComercial")]
    public string NombreComercial { get; set; } = string.Empty;

    [JsonPropertyName("Descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("Categoria")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("Subcategoria")]
    public string Subcategoria { get; set; } = string.Empty;

    [JsonPropertyName("Familia")]
    public string Familia { get; set; } = string.Empty;

    [JsonPropertyName("Marca")]
    public string Marca { get; set; } = string.Empty;

    [JsonPropertyName("UnidadMedidaBase")]
    public string UnidadMedidaBase { get; set; } = string.Empty;

    [JsonPropertyName("AplicaInventario")]
    public bool AplicaInventario { get; set; }

    [JsonPropertyName("AplicaVenta")]
    public bool AplicaVenta { get; set; }

    [JsonPropertyName("AplicaCompra")]
    public bool AplicaCompra { get; set; }

    [JsonPropertyName("ImagenRuta")]
    public string? ImagenRuta { get; set; }

    [JsonPropertyName("Estatus")]
    public int Estatus { get; set; }

    [JsonPropertyName("Presentaciones")]
    public List<PresentacionProducto> Presentaciones { get; set; } = new();

    [JsonPropertyName("Disponibilidad")]
    public List<DisponibilidadProducto> Disponibilidad { get; set; } = new();
}