using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Devoluciones;

// =============================================================
// CHG-0006: devoluciones en caja (solo ventas pagadas en efectivo).
// GET  api/pos/devoluciones/ticket?folio=
// POST api/pos/devoluciones
// La devolución se registra en el módulo CEDIS → Devoluciones y el
// efectivo sale del turno abierto de la caja.
// =============================================================

public class TicketDevolucionResponse
{
    [JsonPropertyName("res")] public int Res { get; set; }
    [JsonPropertyName("msg")] public string? Msg { get; set; }
    [JsonPropertyName("codigo")] public string? Codigo { get; set; }
    [JsonPropertyName("data")] public TicketDevolucionData? Data { get; set; }
}

public class TicketDevolucionData
{
    [JsonPropertyName("venta")] public VentaDevolucion? Venta { get; set; }
    [JsonPropertyName("pagos")] public List<PagoVentaDevolucion> Pagos { get; set; } = new();
    [JsonPropertyName("puede_devolver")] public bool PuedeDevolver { get; set; }
    [JsonPropertyName("motivo_bloqueo")] public string? MotivoBloqueo { get; set; }
    [JsonPropertyName("codigo_bloqueo")] public string? CodigoBloqueo { get; set; }
    [JsonPropertyName("lineas")] public List<LineaVentaDevolucion> Lineas { get; set; } = new();
    [JsonPropertyName("motivos")] public List<MotivoDevolucion> Motivos { get; set; } = new();

    /// <summary>Aviso que no bloquea (por ejemplo: el ticket ya está en una Factura Global).</summary>
    [JsonPropertyName("advertencia")] public string? Advertencia { get; set; }
}

public class VentaDevolucion
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("folio")] public string? Folio { get; set; }
    [JsonPropertyName("folio_ticket")] public string? FolioTicket { get; set; }
    [JsonPropertyName("uuid")] public string? Uuid { get; set; }
    [JsonPropertyName("fecha")] public string? Fecha { get; set; }
    [JsonPropertyName("hora")] public string? Hora { get; set; }
    [JsonPropertyName("estado")] public string? Estado { get; set; }
    [JsonPropertyName("tipo_venta")] public string? TipoVenta { get; set; }
    [JsonPropertyName("total")] public decimal Total { get; set; }
    [JsonPropertyName("cajero")] public string? Cajero { get; set; }
    [JsonPropertyName("caja")] public string? Caja { get; set; }
}

public class PagoVentaDevolucion
{
    [JsonPropertyName("forma")] public string? Forma { get; set; }
    [JsonPropertyName("clave")] public string? Clave { get; set; }
    [JsonPropertyName("importe")] public decimal Importe { get; set; }
}

public class LineaVentaDevolucion
{
    [JsonPropertyName("id_detalle")] public int IdDetalle { get; set; }
    [JsonPropertyName("id_producto")] public int IdProducto { get; set; }
    [JsonPropertyName("id_presentacion")] public int IdPresentacion { get; set; }
    [JsonPropertyName("producto")] public string? Producto { get; set; }
    [JsonPropertyName("presentacion")] public string? Presentacion { get; set; }
    [JsonPropertyName("unidad_medida")] public string? UnidadMedida { get; set; }
    [JsonPropertyName("cantidad_vendida")] public decimal CantidadVendida { get; set; }
    [JsonPropertyName("cantidad_devuelta")] public decimal CantidadDevuelta { get; set; }
    [JsonPropertyName("cantidad_disponible")] public decimal CantidadDisponible { get; set; }
    [JsonPropertyName("precio_unitario")] public decimal PrecioUnitario { get; set; }
    [JsonPropertyName("es_promocion")] public bool EsPromocion { get; set; }
}

public class MotivoDevolucion
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("clave")] public string? Clave { get; set; }
    [JsonPropertyName("descripcion")] public string? Descripcion { get; set; }
    [JsonPropertyName("requiere_observaciones")] public bool RequiereObservaciones { get; set; }

    public override string ToString() => Descripcion ?? Clave ?? string.Empty;
}


public class RegistrarDevolucionRequest
{
    [JsonPropertyName("id_venta")] public int IdVenta { get; set; }

    // El servidor saca el efectivo del turno abierto de esta caja.
    [JsonPropertyName("id_caja")] public int IdCaja { get; set; }
    [JsonPropertyName("id_turno")] public int IdTurno { get; set; }

    [JsonPropertyName("uuid_operacion")] public string UuidOperacion { get; set; } = string.Empty;
    [JsonPropertyName("id_motivo")] public int IdMotivo { get; set; }
    [JsonPropertyName("observaciones")] public string Observaciones { get; set; } = string.Empty;
    [JsonPropertyName("lineas")] public List<LineaDevolucionRequest> Lineas { get; set; } = new();
}

public class LineaDevolucionRequest
{
    [JsonPropertyName("id_detalle")] public int IdDetalle { get; set; }
    [JsonPropertyName("cantidad")] public decimal Cantidad { get; set; }
}


public class RegistrarDevolucionResponse
{
    [JsonPropertyName("res")] public int Res { get; set; }
    [JsonPropertyName("msg")] public string? Msg { get; set; }
    [JsonPropertyName("codigo")] public string? Codigo { get; set; }
    [JsonPropertyName("data")] public DevolucionRegistrada? Data { get; set; }
}

public class DevolucionRegistrada
{
    [JsonPropertyName("id_devolucion")] public int IdDevolucion { get; set; }
    [JsonPropertyName("folio_devolucion")] public string? FolioDevolucion { get; set; }
    [JsonPropertyName("importe_reembolso")] public decimal ImporteReembolso { get; set; }
    [JsonPropertyName("ya_registrada")] public bool YaRegistrada { get; set; }
    [JsonPropertyName("autorizado_por")] public string? AutorizadoPor { get; set; }
    [JsonPropertyName("usuario_autoriza")] public string? UsuarioAutoriza { get; set; }
    [JsonPropertyName("lineas")] public List<LineaDevuelta> Lineas { get; set; } = new();
}

public class LineaDevuelta
{
    [JsonPropertyName("producto")] public string? Producto { get; set; }
    [JsonPropertyName("presentacion")] public string? Presentacion { get; set; }
    [JsonPropertyName("cantidad")] public decimal Cantidad { get; set; }
    [JsonPropertyName("precio_unitario")] public decimal PrecioUnitario { get; set; }
    [JsonPropertyName("importe")] public decimal Importe { get; set; }
}
