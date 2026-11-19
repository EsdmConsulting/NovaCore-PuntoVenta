using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Caja;

public class Caja
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("id_empresa")]
    public int IdEmpresa { get; set; }

    [JsonPropertyName("id_unidad_operativa")]
    public int IdUnidadOperativa { get; set; }

    [JsonPropertyName("unidad_codigo")]
    public string UnidadCodigo { get; set; } = string.Empty;

    [JsonPropertyName("unidad_nombre")]
    public string UnidadNombre { get; set; } = string.Empty;

    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("numero_terminal")]
    public string? NumeroTerminal { get; set; }

    [JsonPropertyName("serie_ticket")]
    public string SerieTicket { get; set; } = string.Empty;

    [JsonPropertyName("ultimo_folio")]
    public int UltimoFolio { get; set; }

    [JsonPropertyName("permite_venta_credito")]
    public bool PermiteVentaCredito { get; set; }

    [JsonPropertyName("permite_precio_manual")]
    public bool PermitePrecioManual { get; set; }

    [JsonPropertyName("permite_descuento")]
    public bool PermiteDescuento { get; set; }

    [JsonPropertyName("estatus")]
    public int Estatus { get; set; }

    [JsonPropertyName("en_uso")]
    public bool EnUso { get; set; }

    // Caja del POS externo ("0002", "0004"…). Solo llega con
    // ?incluir_externas=1 (Prueba de corte); no se vende en ella.
    [JsonPropertyName("caja_externa")]
    public string? CajaExterna { get; set; }

    [JsonPropertyName("es_externa")]
    public bool EsExterna { get; set; }
    
    [JsonIgnore]
    public bool EstaDisponible => !EnUso;
}