using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.TerminalPago;

/// <summary>GET /api/pos/ventas/{id}/pagos-tarjeta: cobros directos con el lector de una venta.</summary>
public class PagosTarjetaVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public List<PagoTarjetaVenta>? Data { get; set; }
}

public class PagoTarjetaVenta
{
    [JsonPropertyName("id_pago")]
    public long IdPago { get; set; }

    [JsonPropertyName("procesador")]
    public string? Procesador { get; set; }

    [JsonPropertyName("referencia_operacion")]
    public string? ReferenciaOperacion { get; set; }

    [JsonPropertyName("importe")]
    public decimal Importe { get; set; }

    [JsonPropertyName("autorizacion")]
    public string? Autorizacion { get; set; }

    [JsonPropertyName("tarjeta_ultimos4")]
    public string? Ultimos4 { get; set; }

    [JsonPropertyName("marca")]
    public string? Marca { get; set; }

    /// <summary>APROBADO, CANCELADO o DEVUELTO.</summary>
    [JsonPropertyName("estado")]
    public string? Estado { get; set; }

    [JsonIgnore]
    public bool Vigente => string.Equals(Estado, "APROBADO", System.StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string Descripcion =>
        string.IsNullOrWhiteSpace(Ultimos4)
            ? $"tarjeta por ${Importe:N2}"
            : $"tarjeta {Marca} ****{Ultimos4} por ${Importe:N2}".Replace("  ", " ");
}
