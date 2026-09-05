using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Turno;

public class CerrarTurnoResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }


    [JsonPropertyName("msg")]
    public string? Msg { get; set; }


    [JsonPropertyName("data")]
    public CerrarTurnoData? Data { get; set; }
}


public class CerrarTurnoData
{
    [JsonPropertyName("id_turno")]
    public int IdTurno { get; set; }


    [JsonPropertyName("id_corte")]
    public int IdCorte { get; set; }


    [JsonPropertyName("estado")]
    public string? Estado { get; set; }


    [JsonPropertyName("resumen")]
    public CerrarTurnoResumen? Resumen { get; set; }
}


public class CerrarTurnoResumen
{
    // ============================================================
    // VENTAS
    // ============================================================

    [JsonPropertyName("total_ventas")]
    public decimal TotalVentas { get; set; }


    [JsonPropertyName("ventas_efectivo")]
    public decimal VentasEfectivo { get; set; }


    [JsonPropertyName("ventas_tarjetas")]
    public decimal VentasTarjetas { get; set; }


    [JsonPropertyName("ventas_transferencias")]
    public decimal VentasTransferencias { get; set; }


    [JsonPropertyName("ventas_credito")]
    public decimal VentasCredito { get; set; }


    // ============================================================
    // ABONOS DE CRÉDITO
    // ============================================================

    [JsonPropertyName("abonos_credito_efectivo")]
    public decimal AbonosCreditoEfectivo { get; set; }


    [JsonPropertyName("abonos_credito_tarjetas")]
    public decimal AbonosCreditoTarjetas { get; set; }


    [JsonPropertyName("abonos_credito_transferencias")]
    public decimal AbonosCreditoTransferencias { get; set; }


    [JsonPropertyName("total_abonos_credito")]
    public decimal TotalAbonosCredito { get; set; }


    // ============================================================
    // TOTALES GENERALES
    // ============================================================

    [JsonPropertyName("total_tarjetas")]
    public decimal TotalTarjetas { get; set; }


    [JsonPropertyName("total_transferencias")]
    public decimal TotalTransferencias { get; set; }


    [JsonPropertyName("total_credito")]
    public decimal TotalCredito { get; set; }


    // ============================================================
    // CAJA
    // ============================================================

    [JsonPropertyName("fondo_inicial")]
    public decimal FondoInicial { get; set; }


    [JsonPropertyName("efectivo_sistema")]
    public decimal EfectivoSistema { get; set; }


    [JsonPropertyName("efectivo_declarado")]
    public decimal EfectivoDeclarado { get; set; }


    [JsonPropertyName("diferencia")]
    public decimal Diferencia { get; set; }


    // ============================================================
    // MOVIMIENTOS
    // ============================================================

    [JsonPropertyName("total_ingresos")]
    public decimal TotalIngresos { get; set; }


    [JsonPropertyName("total_egresos")]
    public decimal TotalEgresos { get; set; }


    [JsonPropertyName("total_retiros")]
    public decimal TotalRetiros { get; set; }


    [JsonPropertyName("total_devoluciones")]
    public decimal TotalDevoluciones { get; set; }
}