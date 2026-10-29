using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Cortes;

// GET /api/pos/cortes/simular?id_caja=&fecha_desde=&fecha_hasta=
// Prueba de corte: no modifica información.

public class SimulacionCorteResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public SimulacionCorteData? Data { get; set; }
}


public class SimulacionCorteData
{
    [JsonPropertyName("caja")]
    public SimulacionCaja? Caja { get; set; }

    [JsonPropertyName("periodo")]
    public SimulacionPeriodo? Periodo { get; set; }

    [JsonPropertyName("ventas")]
    public SimulacionVentas Ventas { get; set; } = new();

    [JsonPropertyName("canceladas")]
    public SimulacionCanceladas Canceladas { get; set; } = new();

    [JsonPropertyName("efectivo")]
    public SimulacionEfectivo Efectivo { get; set; } = new();

    [JsonPropertyName("por_dia")]
    public List<SimulacionDia> PorDia { get; set; } = new();

    [JsonPropertyName("comparacion")]
    public SimulacionComparacion? Comparacion { get; set; }
}


public class SimulacionCaja
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codigo")]
    public string? Codigo { get; set; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("caja_externa")]
    public string? CajaExterna { get; set; }

    [JsonPropertyName("es_externa")]
    public bool EsExterna { get; set; }
}


public class SimulacionPeriodo
{
    [JsonPropertyName("fecha_desde")]
    public string? FechaDesde { get; set; }

    [JsonPropertyName("fecha_hasta")]
    public string? FechaHasta { get; set; }

    [JsonPropertyName("turnos")]
    public int Turnos { get; set; }
}


public class SimulacionVentas
{
    [JsonPropertyName("tickets")]
    public int Tickets { get; set; }

    [JsonPropertyName("total_ventas")]
    public decimal TotalVentas { get; set; }

    [JsonPropertyName("ventas_efectivo")]
    public decimal VentasEfectivo { get; set; }

    [JsonPropertyName("ventas_tarjetas")]
    public decimal VentasTarjetas { get; set; }

    [JsonPropertyName("ventas_transferencias")]
    public decimal VentasTransferencias { get; set; }

    [JsonPropertyName("ventas_otras")]
    public decimal VentasOtras { get; set; }

    [JsonPropertyName("ventas_credito")]
    public decimal VentasCredito { get; set; }

    [JsonPropertyName("tickets_efectivo")]
    public int TicketsEfectivo { get; set; }

    [JsonPropertyName("tickets_tarjetas")]
    public int TicketsTarjetas { get; set; }

    [JsonPropertyName("tickets_transferencias")]
    public int TicketsTransferencias { get; set; }

    [JsonPropertyName("tickets_otras")]
    public int TicketsOtras { get; set; }

    [JsonPropertyName("tickets_credito")]
    public int TicketsCredito { get; set; }
}


public class SimulacionCanceladas
{
    [JsonPropertyName("tickets")]
    public int Tickets { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}


public class SimulacionEfectivo
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


public class SimulacionDia
{
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("tickets")]
    public int Tickets { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}


public class SimulacionComparacion
{
    [JsonPropertyName("caja_externa")]
    public string? CajaExterna { get; set; }

    [JsonPropertyName("cortes")]
    public int Cortes { get; set; }

    [JsonPropertyName("cortes_que_no_cuadran")]
    public int CortesQueNoCuadran { get; set; }

    [JsonPropertyName("total_ventas")]
    public decimal TotalVentas { get; set; }

    [JsonPropertyName("efectivo_sistema")]
    public decimal EfectivoSistema { get; set; }

    [JsonPropertyName("efectivo_caja_externa")]
    public decimal EfectivoCajaExterna { get; set; }

    [JsonPropertyName("diferencia")]
    public decimal Diferencia { get; set; }

    [JsonPropertyName("cuadra")]
    public bool Cuadra { get; set; }

    [JsonPropertyName("detalle")]
    public List<SimulacionCorteExterno> Detalle { get; set; } = new();
}


public class SimulacionCorteExterno
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("folio")]
    public string? Folio { get; set; }

    [JsonPropertyName("id_turno")]
    public int IdTurno { get; set; }

    [JsonPropertyName("fecha_corte")]
    public string? FechaCorte { get; set; }

    [JsonPropertyName("total_ventas")]
    public decimal TotalVentas { get; set; }

    [JsonPropertyName("efectivo_sistema")]
    public decimal EfectivoSistema { get; set; }

    [JsonPropertyName("efectivo_caja_externa")]
    public decimal EfectivoCajaExterna { get; set; }

    [JsonPropertyName("diferencia")]
    public decimal Diferencia { get; set; }

    [JsonPropertyName("cuadra")]
    public bool Cuadra { get; set; }
}
