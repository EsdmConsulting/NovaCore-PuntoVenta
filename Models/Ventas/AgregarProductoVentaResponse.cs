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
    // PRECIO
    // =========================================================

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; set; }

    /*
     * Se mantiene por compatibilidad con el backend actual.
     *
     * Ya NO significa que la cantidad determine el precio.
     *
     * Al agregar una línea nueva:
     * precio_automatico = true
     * significa precio normal/default = MENUDEO.
     */
    [JsonPropertyName("precio_automatico")]
    public bool PrecioAutomatico { get; set; }

    /*
     * Se mantiene temporalmente por compatibilidad.
     *
     * Ya NO representa un límite de precio por cantidad.
     */
    [JsonPropertyName("tipo_precio_maximo")]
    public string? TipoPrecioMaximo { get; set; }

    [JsonPropertyName("tipo_precio_aplicado")]
    public string? TipoPrecioAplicado { get; set; }

    [JsonPropertyName("id_precio_aplicado")]
    public int? IdPrecioAplicado { get; set; }


    // =========================================================
    // CAMPOS LEGACY DE REGLAS
    // =========================================================
    //
    // Se conservan únicamente mientras PHP todavía los devuelve.
    //
    // Con la nueva lógica normalmente vienen:
    //
    // id_regla_aplicada = null
    // cantidad_minima_precio = null
    // usa_reglas_precio = false
    //
    // =========================================================

    [JsonPropertyName("id_regla_aplicada")]
    public int? IdReglaAplicada { get; set; }

    [JsonPropertyName("cantidad_minima_precio")]
    public decimal? CantidadMinimaPrecio { get; set; }

    [JsonPropertyName("usa_reglas_precio")]
    public bool UsaReglasPrecio { get; set; }


    // =========================================================
    // TIPOS DE PRECIO DISPONIBLES
    // =========================================================
    //
    // Ya NO representan niveles por cantidad.
    //
    // Son simplemente los precios configurados para la
    // presentación:
    //
    // MENUDEO
    // MAYOREO
    // DISTRIBUIDOR
    // ESPECIAL
    // etc.
    //
    // =========================================================

    [JsonPropertyName("tipos_disponibles")]
    public List<TipoPrecioDisponible> TiposDisponibles { get; set; }
        = new();


    // =========================================================
    // CAMPOS LEGACY
    // =========================================================
    //
    // Ya no se utilizan para cambiar precio por cantidad.
    // PHP actualmente devuelve null.
    //
    // =========================================================

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
// Representa un precio configurado para la presentación.
//
// IMPORTANTE:
//
// La cantidad NO determina cuál se utiliza.
//
// Ejemplo:
//
// MENUDEO      $400
// MAYOREO      $380
// DISTRIBUIDOR $360
//
// Una línea nueva inicia en MENUDEO.
//
// Los demás pueden seleccionarse manualmente mediante el flujo
// autorizado.
//
// =============================================================

public class TipoPrecioDisponible
{
    /*
     * Legacy.
     *
     * Ya no existe necesariamente una regla asociada.
     * Lo hacemos nullable por compatibilidad.
     */
    [JsonPropertyName("id_regla")]
    public int? IdRegla { get; set; }

    [JsonPropertyName("tipo_precio")]
    public string? TipoPrecio { get; set; }

    /*
     * Legacy.
     *
     * Ya no existe una cantidad mínima para obtener este precio.
     */
    [JsonPropertyName("cantidad_minima")]
    public decimal? CantidadMinima { get; set; }

    [JsonPropertyName("id_precio")]
    public int IdPrecio { get; set; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }
}


// =============================================================
// SIGUIENTE NIVEL DE PRECIO
// =============================================================
//
// Estructura legacy.
//
// Ya no se utiliza para decidir precios por cantidad.
// Se mantiene temporalmente porque algunos contratos actuales
// todavía incluyen "siguiente_nivel": null.
//
// =============================================================

public class SiguienteNivelPrecio
{
    [JsonPropertyName("tipo_precio")]
    public string? TipoPrecio { get; set; }

    [JsonPropertyName("cantidad_minima")]
    public decimal? CantidadMinima { get; set; }

    [JsonPropertyName("faltan")]
    public decimal? Faltan { get; set; }

    [JsonPropertyName("precio")]
    public decimal? Precio { get; set; }

    [JsonPropertyName("mensaje")]
    public string? Mensaje { get; set; }
}
