using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Turno;

public class ResumenTurnoResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public ResumenTurnoData? Data { get; set; }
}


// =============================================================
// DATA
// =============================================================

public class ResumenTurnoData
{
    [JsonPropertyName("turno")]
    public ResumenTurnoInfo? Turno { get; set; }

    [JsonPropertyName("ventas")]
    public ResumenTurnoVentas? Ventas { get; set; }

    [JsonPropertyName("abonos_credito")]
    public ResumenTurnoAbonosCredito? AbonosCredito { get; set; }

    [JsonPropertyName("cobros")]
    public ResumenTurnoCobros? Cobros { get; set; }

    [JsonPropertyName("caja")]
    public ResumenTurnoCaja? Caja { get; set; }

    [JsonPropertyName("graficas")]
    public ResumenTurnoGraficas? Graficas { get; set; }
}


// =============================================================
// TURNO
// =============================================================

public class ResumenTurnoInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("id_caja")]
    public int IdCaja { get; set; }

    [JsonPropertyName("caja_codigo")]
    public string CajaCodigo { get; set; } = string.Empty;

    [JsonPropertyName("caja_nombre")]
    public string CajaNombre { get; set; } = string.Empty;

    [JsonPropertyName("id_usuario_apertura")]
    public int? IdUsuarioApertura { get; set; }

    [JsonPropertyName("usuario_apertura_nombre")]
    public string? UsuarioAperturaNombre { get; set; }

    [JsonPropertyName("usuario_apertura")]
    public string? UsuarioApertura { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("fecha_apertura")]
    public string? FechaApertura { get; set; }

    [JsonPropertyName("fecha_cierre")]
    public string? FechaCierre { get; set; }

    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }

    [JsonPropertyName("fondo_inicial")]
    public decimal FondoInicial { get; set; }

    [JsonPropertyName("ventas_pendientes")]
    public int VentasPendientes { get; set; }

    [JsonPropertyName("puede_cerrar")]
    public bool PuedeCerrar { get; set; }
}


// =============================================================
// VENTAS
// =============================================================

public class ResumenTurnoVentas
{
    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("tickets")]
    public int Tickets { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("contado")]
    public decimal Contado { get; set; }

    [JsonPropertyName("credito")]
    public decimal Credito { get; set; }

    [JsonPropertyName("efectivo")]
    public decimal Efectivo { get; set; }

    [JsonPropertyName("tarjetas")]
    public decimal Tarjetas { get; set; }

    [JsonPropertyName("transferencias")]
    public decimal Transferencias { get; set; }
}


// =============================================================
// ABONOS DE CRÉDITO
// =============================================================

public class ResumenTurnoAbonosCredito
{
    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("efectivo")]
    public decimal Efectivo { get; set; }

    [JsonPropertyName("tarjetas")]
    public decimal Tarjetas { get; set; }

    [JsonPropertyName("transferencias")]
    public decimal Transferencias { get; set; }
}


// =============================================================
// COBROS
// =============================================================

public class ResumenTurnoCobros
{
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("efectivo")]
    public decimal Efectivo { get; set; }

    [JsonPropertyName("tarjetas")]
    public decimal Tarjetas { get; set; }

    [JsonPropertyName("transferencias")]
    public decimal Transferencias { get; set; }
}


// =============================================================
// CAJA
// =============================================================

public class ResumenTurnoCaja
{
    [JsonPropertyName("fondo_inicial")]
    public decimal FondoInicial { get; set; }

    [JsonPropertyName("ventas_efectivo")]
    public decimal VentasEfectivo { get; set; }

    [JsonPropertyName("ingresos")]
    public decimal Ingresos { get; set; }

    [JsonPropertyName("egresos")]
    public decimal Egresos { get; set; }

    [JsonPropertyName("retiros")]
    public decimal Retiros { get; set; }

    [JsonPropertyName("devoluciones")]
    public decimal Devoluciones { get; set; }

    [JsonPropertyName("efectivo_esperado")]
    public decimal EfectivoEsperado { get; set; }
}


// =============================================================
// GRÁFICAS
// =============================================================

public class ResumenTurnoGraficas
{
    [JsonPropertyName("formas_pago")]
    public List<ResumenTurnoGraficaFormaPago> FormasPago { get; set; } = new();

    [JsonPropertyName("tipo_venta")]
    public List<ResumenTurnoGraficaTipoVenta> TipoVenta { get; set; } = new();

    [JsonPropertyName("ventas_por_hora")]
    public List<ResumenTurnoGraficaVentaHora> VentasPorHora { get; set; } = new();
}


// =============================================================
// GRÁFICA: FORMAS DE PAGO
// =============================================================

public class ResumenTurnoGraficaFormaPago
{
    [JsonPropertyName("clave")]
    public string Clave { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("porcentaje")]
    public decimal Porcentaje { get; set; }
}


// =============================================================
// GRÁFICA: TIPO DE VENTA
// =============================================================

public class ResumenTurnoGraficaTipoVenta
{
    [JsonPropertyName("clave")]
    public string Clave { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("porcentaje")]
    public decimal Porcentaje { get; set; }
}


// =============================================================
// GRÁFICA: VENTAS POR HORA
// =============================================================

public class ResumenTurnoGraficaVentaHora
{
    [JsonPropertyName("hora")]
    public string Hora { get; set; } = string.Empty;

    [JsonPropertyName("cantidad_ventas")]
    public int CantidadVentas { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}