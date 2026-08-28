using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class CrearVentaRequest
{
    [JsonPropertyName("id_empresa")]
    public int IdEmpresa { get; set; }

    [JsonPropertyName("id_unidad_operativa")]
    public int IdUnidadOperativa { get; set; }

    [JsonPropertyName("id_caja")]
    public int IdCaja { get; set; }

    [JsonPropertyName("id_turno")]
    public int IdTurno { get; set; }

    [JsonPropertyName("id_cliente")]
    public int? IdCliente { get; set; }

    [JsonPropertyName("tipo_cliente")]
    public string TipoCliente { get; set; } = "PUBLICO_GENERAL";

    [JsonPropertyName("tipo_venta")]
    public string TipoVenta { get; set; } = "CONTADO";

    [JsonPropertyName("requiere_factura")]
    public bool RequiereFactura { get; set; }

    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    [JsonPropertyName("observaciones")]
    public string Observaciones { get; set; } = string.Empty;
}