using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

public class VentaActualResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public VentaActualData? Data { get; set; }
}

public class VentaActualData
{
    [JsonPropertyName("venta")]
    public VentaActual? Venta { get; set; }

    [JsonPropertyName("detalles")]
    public List<VentaActualDetalle> Detalles { get; set; }
        = new();
}

public class VentaActual
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
        = string.Empty;

    [JsonPropertyName("folio")]
    public string Folio { get; set; }
        = string.Empty;

    [JsonPropertyName("id_empresa")]
    public int IdEmpresa { get; set; }

    [JsonPropertyName("id_unidad_operativa")]
    public int IdUnidadOperativa { get; set; }

    [JsonPropertyName("id_caja")]
    public int IdCaja { get; set; }

    [JsonPropertyName("id_turno")]
    public int IdTurno { get; set; }

    [JsonPropertyName("tipo_cliente")]
    public string TipoCliente { get; set; }
        = string.Empty;

    [JsonPropertyName("tipo_venta")]
    public string TipoVenta { get; set; }
        = string.Empty;

    [JsonPropertyName("requiere_factura")]
    public bool RequiereFactura { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("descuento")]
    public decimal Descuento { get; set; }

    [JsonPropertyName("total_iva")]
    public decimal TotalIva { get; set; }

    [JsonPropertyName("total_ieps")]
    public decimal TotalIeps { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("monto_pagado")]
    public decimal MontoPagado { get; set; }

    [JsonPropertyName("saldo")]
    public decimal Saldo { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; }
        = string.Empty;
}

public class VentaActualDetalle
{
    [JsonPropertyName("id_detalle")]
    public int IdDetalle { get; set; }

    [JsonPropertyName("numero_linea")]
    public int NumeroLinea { get; set; }

    [JsonPropertyName("id_producto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("id_presentacion")]
    public int IdPresentacion { get; set; }

    [JsonPropertyName("descripcion_snapshot")]
    public string DescripcionSnapshot { get; set; }
        = string.Empty;


    // =========================================================
    // CANTIDAD
    // =========================================================

    [JsonPropertyName("cantidad_comercial")]
    public decimal CantidadComercial { get; set; }

    [JsonPropertyName("factor_conversion")]
    public decimal FactorConversion { get; set; }

    [JsonPropertyName("cantidad_base")]
    public decimal CantidadBase { get; set; }


    // =========================================================
    // PRECIOS / TOTALES
    // =========================================================

    [JsonPropertyName("precio_lista")]
    public decimal PrecioLista { get; set; }

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; set; }

    [JsonPropertyName("descuento_porcentaje")]
    public decimal DescuentoPorcentaje { get; set; }

    [JsonPropertyName("descuento_importe")]
    public decimal DescuentoImporte { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("total_iva")]
    public decimal TotalIva { get; set; }

    [JsonPropertyName("total_ieps")]
    public decimal TotalIeps { get; set; }

    [JsonPropertyName("total_linea")]
    public decimal TotalLinea { get; set; }


    // =========================================================
    // ESTADO DEL PRECIO DE LA LÍNEA
    // =========================================================
    //
    // Estos valores son MUY importantes al recuperar una
    // venta BORRADOR.
    //
    // Ejemplo:
    //
    // precio_automatico = false
    // tipo_precio_maximo = "MAYOREO"
    // tipo_precio_aplicado = "MAYOREO"
    //
    // significa que el cajero había forzado MAYOREO.
    //
    // No debemos perder esa configuración al volver a
    // abrir el POS.
    // =========================================================

    [JsonPropertyName("precio_automatico")]
    public bool PrecioAutomatico { get; set; }

    [JsonPropertyName("tipo_precio_maximo")]
    public string? TipoPrecioMaximo { get; set; }

    [JsonPropertyName("tipo_precio_aplicado")]
    public string? TipoPrecioAplicado { get; set; }

    [JsonPropertyName("id_precio_aplicado")]
    public int? IdPrecioAplicado { get; set; }


    // =========================================================
    // ESTADO
    // =========================================================

    [JsonPropertyName("estado")]
    public string Estado { get; set; }
        = string.Empty;


    // =========================================================
    // PRODUCTO
    // =========================================================

    [JsonPropertyName("codigo")]
    public string Codigo { get; set; }
        = string.Empty;

    [JsonPropertyName("codigo_barras")]
    public string CodigoBarras { get; set; }
        = string.Empty;

    [JsonPropertyName("nombre_comercial")]
    public string NombreComercial { get; set; }
        = string.Empty;

    [JsonPropertyName("nombre_presentacion")]
    public string NombrePresentacion { get; set; }
        = string.Empty;

    [JsonPropertyName("unidad_medida")]
    public string UnidadMedida { get; set; }
        = string.Empty;

    [JsonPropertyName("presentacion_codigo_barras")]
    public string PresentacionCodigoBarras { get; set; }
        = string.Empty;


    // =========================================================
    // INVENTARIO
    // =========================================================

    [JsonPropertyName("existencia")]
    public decimal Existencia { get; set; }

    [JsonPropertyName("existencia_apartada")]
    public decimal ExistenciaApartada { get; set; }

    [JsonPropertyName("existencia_disponible")]
    public decimal ExistenciaDisponible { get; set; }

    [JsonPropertyName("maximo_linea")]
    public decimal MaximoLinea { get; set; }
}