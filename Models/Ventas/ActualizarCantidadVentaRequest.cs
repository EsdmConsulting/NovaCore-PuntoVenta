using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;


// =============================================================
// REQUEST
// =============================================================

public class ActualizarCantidadVentaRequest
{
    // ---------------------------------------------------------
    // CANTIDAD
    // ---------------------------------------------------------
    //
    // Nullable porque update_detalle.php permite cambiar
    // únicamente el tipo de precio sin modificar la cantidad.
    //
    // Si es null, no se envía.
    // ---------------------------------------------------------

    [JsonPropertyName("cantidad_comercial")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? CantidadComercial { get; set; }


    // ---------------------------------------------------------
    // TIPO DE PRECIO
    // ---------------------------------------------------------
    //
    // Este es el campo ACTUAL utilizado por update_detalle.php
    // para cambiar manualmente el precio de una línea.
    //
    // Ejemplos:
    //
    // tipo_precio = "MENUDEO"
    // tipo_precio = "MAYOREO"
    // tipo_precio = "BODEGAS"
    // tipo_precio = "DISTRIBUIDOR"
    // tipo_precio = "ESPECIAL"
    //
    // El nombre es dinámico. El backend valida que exista
    // un precio vigente para la presentación.
    //
    // Si es null, no se envía y el backend conserva
    // el tipo de precio que actualmente tiene la línea.
    // ---------------------------------------------------------

    [JsonPropertyName("tipo_precio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TipoPrecio { get; set; }


    // ---------------------------------------------------------
    // PRECIO AUTOMÁTICO
    // ---------------------------------------------------------
    //
    // Se conserva temporalmente por compatibilidad con código
    // anterior del POS.
    //
    // El nuevo flujo de selección manual NO debe utilizar esta
    // propiedad para indicar qué precio quiere aplicar.
    //
    // Si es null, no se envía.
    // ---------------------------------------------------------

    [JsonPropertyName("precio_automatico")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PrecioAutomatico { get; set; }


    // ---------------------------------------------------------
    // TIPO DE PRECIO MÁXIMO
    // ---------------------------------------------------------
    //
    // Se conserva temporalmente por compatibilidad.
    //
    // El backend actual calcula este valor a partir del
    // tipo_precio seleccionado.
    //
    // No debe utilizarse para seleccionar MAYOREO,
    // BODEGAS, DISTRIBUIDOR, etc.
    //
    // IMPORTANTE:
    // No ignoramos null para conservar compatibilidad con
    // código anterior que pudiera necesitar enviarlo
    // explícitamente.
    // ---------------------------------------------------------

    [JsonPropertyName("tipo_precio_maximo")]
    public string? TipoPrecioMaximo { get; set; }
}


// =============================================================
// RESPONSE
// =============================================================

public class ActualizarCantidadVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public ActualizarCantidadVentaData? Data { get; set; }
}


// =============================================================
// DATA
// =============================================================

public class ActualizarCantidadVentaData
{
    [JsonPropertyName("detalle")]
    public DetalleCantidadActualizado? Detalle { get; set; }


    // Información completa del precio resuelto por el backend.
    [JsonPropertyName("precio")]
    public PrecioCantidadActualizado? Precio { get; set; }


    [JsonPropertyName("inventario")]
    public InventarioCantidadActualizado? Inventario { get; set; }
}


// =============================================================
// DETALLE ACTUALIZADO
// =============================================================

public class DetalleCantidadActualizado
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("id_producto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("id_presentacion")]
    public int IdPresentacion { get; set; }


    // ---------------------------------------------------------
    // CANTIDAD
    // ---------------------------------------------------------

    [JsonPropertyName("cantidad_comercial")]
    public decimal CantidadComercial { get; set; }

    [JsonPropertyName("factor_conversion")]
    public decimal FactorConversion { get; set; }

    [JsonPropertyName("cantidad_base")]
    public decimal CantidadBase { get; set; }


    // ---------------------------------------------------------
    // PRECIO
    // ---------------------------------------------------------

    [JsonPropertyName("precio_lista")]
    public decimal PrecioLista { get; set; }

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; set; }


    // ---------------------------------------------------------
    // DESCUENTOS / TOTALES
    // ---------------------------------------------------------

    [JsonPropertyName("descuento_porcentaje")]
    public decimal DescuentoPorcentaje { get; set; }

    [JsonPropertyName("descuento_importe")]
    public decimal DescuentoImporte { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("total_linea")]
    public decimal TotalLinea { get; set; }


    // ---------------------------------------------------------
    // CONFIGURACIÓN DE PRECIO GUARDADA EN LA LÍNEA
    // ---------------------------------------------------------

    [JsonPropertyName("precio_automatico")]
    public bool PrecioAutomatico { get; set; }

    [JsonPropertyName("tipo_precio_maximo")]
    public string? TipoPrecioMaximo { get; set; }

    [JsonPropertyName("tipo_precio_aplicado")]
    public string? TipoPrecioAplicado { get; set; }

    [JsonPropertyName("id_precio_aplicado")]
    public int? IdPrecioAplicado { get; set; }
}


// =============================================================
// INFORMACIÓN DEL PRECIO RESUELTO
// =============================================================

public class PrecioCantidadActualizado
{
    // ---------------------------------------------------------
    // CONFIGURACIÓN
    // ---------------------------------------------------------

    [JsonPropertyName("precio_automatico")]
    public bool PrecioAutomatico { get; set; }

    [JsonPropertyName("tipo_precio_maximo")]
    public string? TipoPrecioMaximo { get; set; }


    // ---------------------------------------------------------
    // PRECIO QUE REALMENTE SE APLICÓ
    // ---------------------------------------------------------

    [JsonPropertyName("tipo_precio_aplicado")]
    public string? TipoPrecioAplicado { get; set; }

    [JsonPropertyName("id_precio_aplicado")]
    public int? IdPrecioAplicado { get; set; }

    [JsonPropertyName("id_regla_aplicada")]
    public int? IdReglaAplicada { get; set; }

    [JsonPropertyName("cantidad_minima")]
    public decimal? CantidadMinima { get; set; }

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; set; }

    [JsonPropertyName("usa_reglas_precio")]
    public bool UsaReglasPrecio { get; set; }


    // ---------------------------------------------------------
    // TODOS LOS TIPOS DISPONIBLES
    // ---------------------------------------------------------

    [JsonPropertyName("tipos_disponibles")]
    public List<TipoPrecioDisponible> TiposDisponibles { get; set; } = new();

    [JsonPropertyName("nivel_automatico_por_cantidad")]
    public TipoPrecioDisponible? NivelAutomaticoPorCantidad { get; set; }

    [JsonPropertyName("siguiente_nivel")]
    public SiguienteNivelPrecio? SiguienteNivel { get; set; }
}


// =============================================================
// INVENTARIO ACTUALIZADO
// =============================================================

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