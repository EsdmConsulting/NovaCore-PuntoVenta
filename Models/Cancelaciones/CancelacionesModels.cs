using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Cancelaciones;


// ============================================================
// AUTORIZAR ADMINISTRADOR
// POST /api/pos/cancelaciones/autorizar
// ============================================================

public class AutorizarCancelacionRequest
{
    [JsonPropertyName("usuario")]
    public string Usuario { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}


public class AutorizarCancelacionResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public AutorizarCancelacionData? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}


public class AutorizarCancelacionData
{
    [JsonPropertyName("id_usuario")]
    public int IdUsuario { get; set; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("usuario")]
    public string? Usuario { get; set; }

    [JsonPropertyName("rol")]
    public string? Rol { get; set; }

    [JsonPropertyName("duracion_segundos")]
    public int DuracionSegundos { get; set; }
}


// ============================================================
// LISTADO DE VENTAS
// GET /api/pos/cancelaciones
// ============================================================

public class CancelacionesListResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public CancelacionesListData? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}


public class CancelacionesListData
{
    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("ventas")]
    public List<VentaCancelacionItem> Ventas { get; set; } = new();
}


public class VentaCancelacionItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("folio")]
    public string? Folio { get; set; }

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
    public string? TipoCliente { get; set; }

    [JsonPropertyName("tipo_venta")]
    public string? TipoVenta { get; set; }

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
    public string? Estado { get; set; }

    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    [JsonPropertyName("hora")]
    public string? Hora { get; set; }

    [JsonPropertyName("id_usuario")]
    public int IdUsuario { get; set; }

    [JsonPropertyName("usuario_registro")]
    public string? UsuarioRegistro { get; set; }

    [JsonPropertyName("id_usuario_cancelacion")]
    public int? IdUsuarioCancelacion { get; set; }

    [JsonPropertyName("usuario_cancelacion")]
    public string? UsuarioCancelacion { get; set; }

    [JsonPropertyName("fecha_cancelacion")]
    public string? FechaCancelacion { get; set; }

    [JsonPropertyName("motivo_cancelacion")]
    public string? MotivoCancelacion { get; set; }


    // ========================================================
    // PROPIEDADES PARA UI
    // ========================================================

    [JsonIgnore]
    public bool EstaCancelada =>
        string.Equals(
            Estado,
            "CANCELADA",
            StringComparison.OrdinalIgnoreCase
        );


    [JsonIgnore]
    public bool PuedeCancelar =>
        string.Equals(
            Estado,
            "FINALIZADA",
            StringComparison.OrdinalIgnoreCase
        );


    [JsonIgnore]
    public string TextoTotal =>
        $"${Total:N2}";


    [JsonIgnore]
    public string TextoEstado =>
        Estado ?? string.Empty;


    [JsonIgnore]
    public string TextoFechaHora
    {
        get
        {
            if (
                string.IsNullOrWhiteSpace(Fecha) &&
                string.IsNullOrWhiteSpace(Hora)
            )
            {
                return string.Empty;
            }

            return $"{Fecha} {Hora}".Trim();
        }
    }
}


// ============================================================
// DETALLE DE VENTA
// GET /api/pos/cancelaciones/{id}
// ============================================================

public class CancelacionDetalleResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public CancelacionDetalleData? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}


public class CancelacionDetalleData
{
    /*
     * detalle.php devuelve "venta" usando SELECT v.*.
     *
     * Utilizamos VentaCancelacionDetalle para permitir
     * recuperar también información de cancelación.
     */
    [JsonPropertyName("venta")]
    public VentaCancelacionDetalle? Venta { get; set; }

    [JsonPropertyName("detalles")]
    public List<DetalleProductoCancelacion> Detalles { get; set; } = new();

    [JsonPropertyName("puede_cancelar")]
    public bool PuedeCancelar { get; set; }
}


public class VentaCancelacionDetalle
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("folio")]
    public string? Folio { get; set; }

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
    public string? TipoCliente { get; set; }

    [JsonPropertyName("tipo_venta")]
    public string? TipoVenta { get; set; }

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
    public string? Estado { get; set; }

    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    [JsonPropertyName("hora")]
    public string? Hora { get; set; }

    [JsonPropertyName("id_usuario")]
    public int IdUsuario { get; set; }

    [JsonPropertyName("usuario_registro")]
    public string? UsuarioRegistro { get; set; }

    [JsonPropertyName("id_usuario_cancelacion")]
    public int? IdUsuarioCancelacion { get; set; }

    [JsonPropertyName("usuario_cancelacion")]
    public string? UsuarioCancelacion { get; set; }

    [JsonPropertyName("fecha_cancelacion")]
    public string? FechaCancelacion { get; set; }
    
    [JsonPropertyName("motivo_cancelacion")]
    public string? MotivoCancelacion { get; set; }


    [JsonIgnore]
    public string TextoTotal =>
        $"${Total:N2}";
}


public class DetalleProductoCancelacion
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
    public string? DescripcionSnapshot { get; set; }

    [JsonPropertyName("cantidad_comercial")]
    public decimal CantidadComercial { get; set; }

    [JsonPropertyName("factor_conversion")]
    public decimal FactorConversion { get; set; }

    [JsonPropertyName("cantidad_base")]
    public decimal CantidadBase { get; set; }

    [JsonPropertyName("precio_lista")]
    public decimal PrecioLista { get; set; }

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

    [JsonPropertyName("estado")]
    public string? Estado { get; set; }

    [JsonPropertyName("codigo")]
    public string? Codigo { get; set; }

    [JsonPropertyName("codigo_barras")]
    public string? CodigoBarras { get; set; }

    [JsonPropertyName("nombre_comercial")]
    public string? NombreComercial { get; set; }

    [JsonPropertyName("nombre_presentacion")]
    public string? NombrePresentacion { get; set; }

    [JsonPropertyName("unidad_medida")]
    public string? UnidadMedida { get; set; }

    [JsonPropertyName("presentacion_codigo_barras")]
    public string? PresentacionCodigoBarras { get; set; }


    [JsonIgnore]
    public string NombreMostrar =>
        !string.IsNullOrWhiteSpace(DescripcionSnapshot)
            ? DescripcionSnapshot
            : NombreComercial ?? "Producto";


    [JsonIgnore]
    public string TextoCantidad =>
        $"{CantidadComercial:N2} {UnidadMedida}".Trim();


    [JsonIgnore]
    public string TextoPrecio =>
        $"${PrecioUnitario:N2}";


    [JsonIgnore]
    public string TextoTotal =>
        $"${TotalLinea:N2}";
}


// ============================================================
// CANCELAR VENTA
// POST /api/pos/cancelaciones/{id}/cancelar
// ============================================================

public class CancelarVentaRequest
{
    [JsonPropertyName("motivo")]
    public string Motivo { get; set; } = string.Empty;
}


public class CancelarVentaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public CancelarVentaData? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}


public class CancelarVentaData
{
    [JsonPropertyName("id_venta")]
    public int IdVenta { get; set; }

    [JsonPropertyName("folio")]
    public string? Folio { get; set; }

    [JsonPropertyName("estado")]
    public string? Estado { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("motivo_cancelacion")]
    public string? MotivoCancelacion { get; set; }

    [JsonPropertyName("administrador")]
    public AdministradorCancelacion? Administrador { get; set; }

    [JsonPropertyName("inventarios")]
    public List<InventarioDevueltoCancelacion> Inventarios { get; set; } = new();
}


public class AdministradorCancelacion
{
    [JsonPropertyName("id_usuario")]
    public int IdUsuario { get; set; }

    [JsonPropertyName("usuario")]
    public string? Usuario { get; set; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; }
}


public class InventarioDevueltoCancelacion
{
    [JsonPropertyName("id_producto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("id_presentacion")]
    public int IdPresentacion { get; set; }

    [JsonPropertyName("cantidad_devuelta")]
    public decimal CantidadDevuelta { get; set; }

    [JsonPropertyName("existencia")]
    public decimal Existencia { get; set; }

    [JsonPropertyName("existencia_apartada")]
    public decimal ExistenciaApartada { get; set; }

    [JsonPropertyName("existencia_disponible")]
    public decimal ExistenciaDisponible { get; set; }
}


public class CerrarAutorizacionResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public CerrarAutorizacionData? Data { get; set; }
}

public class CerrarAutorizacionData
{
    [JsonPropertyName("autorizacion_cancelaciones_activa")]
    public bool AutorizacionCancelacionesActiva { get; set; }
}