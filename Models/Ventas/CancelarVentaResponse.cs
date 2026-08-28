using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class CancelarVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

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
}