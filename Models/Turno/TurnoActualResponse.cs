using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Turno;

public class TurnoActualResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public TurnoActual? Data { get; set; }
}

public class TurnoActual
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

    [JsonPropertyName("fondo_inicial")]
    public decimal FondoInicial { get; set; }

    [JsonPropertyName("efectivo_sistema")]
    public decimal? EfectivoSistema { get; set; }

    [JsonPropertyName("efectivo_declarado")]
    public decimal? EfectivoDeclarado { get; set; }

    [JsonPropertyName("diferencia")]
    public decimal? Diferencia { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }

    [JsonPropertyName("fecha_apertura")]
    public string? FechaApertura { get; set; }

    [JsonPropertyName("fecha_cierre")]
    public string? FechaCierre { get; set; }

    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    [JsonPropertyName("hora")]
    public string? Hora { get; set; }
}