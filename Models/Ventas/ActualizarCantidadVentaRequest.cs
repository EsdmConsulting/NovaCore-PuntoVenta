using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class ActualizarCantidadVentaRequest
{
    [JsonPropertyName("cantidad_comercial")]
    public decimal CantidadComercial { get; set; }
}

public class ActualizarCantidadVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public ActualizarCantidadVentaData? Data { get; set; }
}

public class ActualizarCantidadVentaData
{
    [JsonPropertyName("detalle")]
    public DetalleCantidadActualizado? Detalle { get; set; }

    [JsonPropertyName("inventario")]
    public InventarioCantidadActualizado? Inventario { get; set; }
}

public class DetalleCantidadActualizado
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("id_producto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("id_presentacion")]
    public int IdPresentacion { get; set; }

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

    [JsonPropertyName("total_linea")]
    public decimal TotalLinea { get; set; }
}

public class InventarioCantidadActualizado
{
    [JsonPropertyName("existencia")]
    public decimal Existencia { get; set; }

    [JsonPropertyName("existencia_apartada")]
    public decimal ExistenciaApartada { get; set; }

    [JsonPropertyName("existencia_disponible")]
    public decimal ExistenciaDisponible { get; set; }

    [JsonPropertyName("maximo_linea")]
    public decimal MaximoLinea { get; set; }
}