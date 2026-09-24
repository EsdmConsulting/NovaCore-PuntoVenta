using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class EliminarProductoVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    // =========================================================
    // MERCADO PAGO / CONCILIACIÓN
    // =========================================================

    [JsonPropertyName("requiere_conciliacion_mp")]
    public bool RequiereConciliacionMp { get; set; }

    [JsonPropertyName("id_operacion_mp")]
    public long? IdOperacionMp { get; set; }

    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    [JsonPropertyName("status")]
    public string? StatusMp { get; set; }

    [JsonPropertyName("status_detail")]
    public string? StatusDetailMp { get; set; }

    // =========================================================
    // RESULTADO NORMAL
    // =========================================================

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

    [JsonPropertyName("operaciones_mp_eliminadas")]
    public int OperacionesMpEliminadas { get; set; }

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