using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Cortes;

public class CortesListResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public CortesListData? Data { get; set; }
}


public class CortesListData
{
    [JsonPropertyName("cortes")]
    public List<CorteListItem> Cortes { get; set; }
        = new();

    [JsonPropertyName("resumen")]
    public CortesListResumen? Resumen { get; set; }

    [JsonPropertyName("filtros")]
    public CortesListFiltros? Filtros { get; set; }
}


public class CorteListItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("folio")]
    public string? Folio { get; set; }

    [JsonPropertyName("id_turno")]
    public int IdTurno { get; set; }

    [JsonPropertyName("id_caja")]
    public int IdCaja { get; set; }

    [JsonPropertyName("caja_codigo")]
    public string? CajaCodigo { get; set; }

    [JsonPropertyName("caja_nombre")]
    public string? CajaNombre { get; set; }

    [JsonPropertyName("fecha_corte")]
    public string? FechaCorte { get; set; }

    [JsonPropertyName("estado")]
    public string? Estado { get; set; }

    [JsonPropertyName("total_ventas")]
    public decimal TotalVentas { get; set; }

    [JsonPropertyName("efectivo_esperado")]
    public decimal EfectivoEsperado { get; set; }

    [JsonPropertyName("efectivo_declarado")]
    public decimal EfectivoDeclarado { get; set; }

    [JsonPropertyName("diferencia")]
    public decimal Diferencia { get; set; }

    [JsonPropertyName("estado_diferencia")]
    public string? EstadoDiferencia { get; set; }

    [JsonPropertyName("usuario_apertura")]
    public string? UsuarioApertura { get; set; }

    [JsonPropertyName("usuario_cierre")]
    public string? UsuarioCierre { get; set; }
}


public class CortesListResumen
{
    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("total_ventas")]
    public decimal TotalVentas { get; set; }

    [JsonPropertyName("total_efectivo_esperado")]
    public decimal TotalEfectivoEsperado { get; set; }

    [JsonPropertyName("total_efectivo_declarado")]
    public decimal TotalEfectivoDeclarado { get; set; }

    [JsonPropertyName("total_diferencia")]
    public decimal TotalDiferencia { get; set; }
}


public class CortesListFiltros
{
    [JsonPropertyName("id_caja")]
    public int? IdCaja { get; set; }

    [JsonPropertyName("id_turno")]
    public int? IdTurno { get; set; }

    [JsonPropertyName("fecha_desde")]
    public string? FechaDesde { get; set; }

    [JsonPropertyName("fecha_hasta")]
    public string? FechaHasta { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}