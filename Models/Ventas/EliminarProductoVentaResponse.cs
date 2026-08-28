using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class EliminarProductoVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public EliminarProductoVentaData? Data { get; set; }
}

public class EliminarProductoVentaData
{
    [JsonPropertyName("id_detalle")]
    public int IdDetalle { get; set; }

    [JsonPropertyName("id_venta")]
    public int IdVenta { get; set; }

    [JsonPropertyName("folio")]
    public string? Folio { get; set; }

    [JsonPropertyName("cantidad_liberada")]
    public decimal CantidadLiberada { get; set; }

    [JsonPropertyName("detalles_restantes")]
    public int DetallesRestantes { get; set; }

    [JsonPropertyName("venta_eliminada")]
    public bool VentaEliminada { get; set; }

    [JsonPropertyName("inventario")]
    public InventarioEliminarProducto? Inventario { get; set; }
}

public class InventarioEliminarProducto
{
    [JsonPropertyName("existencia")]
    public decimal Existencia { get; set; }

    [JsonPropertyName("existencia_apartada")]
    public decimal ExistenciaApartada { get; set; }

    [JsonPropertyName("existencia_disponible")]
    public decimal ExistenciaDisponible { get; set; }
}