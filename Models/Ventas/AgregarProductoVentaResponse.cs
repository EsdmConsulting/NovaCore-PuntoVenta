using System.Collections.Generic;
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
    
    [JsonPropertyName("unidad_medida")]
    public string? UnidadMedida { get; set; }


    // =========================================================
    // CANTIDADES
    // =========================================================

    [JsonPropertyName("cantidad_comercial")]
    public decimal CantidadComercial { get; set; }

    [JsonPropertyName("factor_conversion")]
    public decimal FactorConversion { get; set; }

    [JsonPropertyName("cantidad_base")]
    public decimal CantidadBase { get; set; }


    // =========================================================
    // PRECIO APLICADO
    // =========================================================

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; set; }

    [JsonPropertyName("precio_automatico")]
    public bool PrecioAutomatico { get; set; }

    [JsonPropertyName("tipo_precio_maximo")]
    public string? TipoPrecioMaximo { get; set; }

    [JsonPropertyName("tipo_precio_aplicado")]
    public string? TipoPrecioAplicado { get; set; }

    [JsonPropertyName("id_precio_aplicado")]
    public int? IdPrecioAplicado { get; set; }

    [JsonPropertyName("id_regla_aplicada")]
    public int? IdReglaAplicada { get; set; }

    [JsonPropertyName("cantidad_minima_precio")]
    public decimal CantidadMinimaPrecio { get; set; }

    [JsonPropertyName("usa_reglas_precio")]
    public bool UsaReglasPrecio { get; set; }


    // =========================================================
    // NIVELES DE PRECIO DISPONIBLES
    // =========================================================

    [JsonPropertyName("tipos_disponibles")]
    public List<TipoPrecioDisponible> TiposDisponibles { get; set; } = new();

    [JsonPropertyName("nivel_automatico_por_cantidad")]
    public TipoPrecioDisponible? NivelAutomaticoPorCantidad { get; set; }

    [JsonPropertyName("siguiente_nivel")]
    public SiguienteNivelPrecio? SiguienteNivel { get; set; }


    // =========================================================
    // IMPORTES
    // =========================================================

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("descuento")]
    public decimal Descuento { get; set; }

    [JsonPropertyName("total_linea")]
    public decimal TotalLinea { get; set; }


    // =========================================================
    // INVENTARIO
    // =========================================================

    [JsonPropertyName("existencia")]
    public decimal Existencia { get; set; }

    [JsonPropertyName("existencia_apartada")]
    public decimal ExistenciaApartada { get; set; }

    [JsonPropertyName("existencia_disponible")]
    public decimal ExistenciaDisponible { get; set; }
}


// =============================================================
// TIPO DE PRECIO DISPONIBLE
// =============================================================
//
// Representa uno de los niveles que el backend permite
// utilizar para la presentación.
//
// Ejemplo:
//
// MENUDEO
// cantidad_minima = 1
// precio = 21
//
// MAYOREO
// cantidad_minima = 6
// precio = 18
//
// Los nombres NO están hardcodeados.
// =============================================================

public class TipoPrecioDisponible
{
    [JsonPropertyName("id_regla")]
    public int IdRegla { get; set; }

    [JsonPropertyName("tipo_precio")]
    public string? TipoPrecio { get; set; }

    [JsonPropertyName("cantidad_minima")]
    public decimal CantidadMinima { get; set; }

    [JsonPropertyName("id_precio")]
    public int IdPrecio { get; set; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }
}


// =============================================================
// SIGUIENTE NIVEL DE PRECIO
// =============================================================
//
// Ejemplo:
//
// {
//     "tipo_precio": "MAYOREO",
//     "cantidad_minima": 6,
//     "faltan": 2,
//     "precio": 18,
//     "mensaje": "Faltan 2 para MAYOREO"
// }
// =============================================================

public class SiguienteNivelPrecio
{
    [JsonPropertyName("tipo_precio")]
    public string? TipoPrecio { get; set; }

    [JsonPropertyName("cantidad_minima")]
    public decimal CantidadMinima { get; set; }

    [JsonPropertyName("faltan")]
    public decimal Faltan { get; set; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }

    [JsonPropertyName("mensaje")]
    public string? Mensaje { get; set; }
}