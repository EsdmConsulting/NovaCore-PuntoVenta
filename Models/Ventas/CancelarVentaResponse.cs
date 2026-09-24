using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class CancelarVentaResponse
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
    public CancelarVentaData? Data { get; set; }
}

public class CancelarVentaData
{
    [JsonPropertyName("id_venta")]
    public int IdVenta { get; set; }

    [JsonPropertyName("folio")]
    public string? Folio { get; set; }

    [JsonPropertyName("detalles_liberados")]
    public int DetallesLiberados { get; set; }

    [JsonPropertyName("detalles_eliminados")]
    public int DetallesEliminados { get; set; }

    [JsonPropertyName("venta_eliminada")]
    public bool VentaEliminada { get; set; }

    [JsonPropertyName("operaciones_mp_eliminadas")]
    public int OperacionesMpEliminadas { get; set; }
}