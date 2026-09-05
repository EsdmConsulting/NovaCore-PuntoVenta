using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Turno;

public class AbrirTurnoResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("codigo")]
    public string? Codigo { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public AbrirTurnoData? Data { get; set; }


    // =========================================================
    // AYUDAS
    // =========================================================

    [JsonIgnore]
    public bool FueAperturaCorrecta =>
        Res == 1 &&
        string.Equals(
            Codigo,
            "TURNO_ABIERTO",
            System.StringComparison.OrdinalIgnoreCase);


    [JsonIgnore]
    public bool YaExisteTurno =>
        string.Equals(
            Codigo,
            "TURNO_YA_ABIERTO",
            System.StringComparison.OrdinalIgnoreCase);
}


// =============================================================
// DATA
// =============================================================

public class AbrirTurnoData
{
    /*
     * Cuando el backend crea un turno nuevo:
     *
     * data: {
     *     "turno": { ... }
     * }
     */
    [JsonPropertyName("turno")]
    public TurnoAbierto? Turno { get; set; }


    /*
     * Cuando la caja ya tiene un turno:
     *
     * data: {
     *     "turno_existente": { ... }
     * }
     */
    [JsonPropertyName("turno_existente")]
    public TurnoAbierto? TurnoExistente { get; set; }


    [JsonPropertyName("mismo_usuario")]
    public bool MismoUsuario { get; set; }


    [JsonPropertyName("puede_continuar")]
    public bool PuedeContinuar { get; set; }


    [JsonPropertyName("requiere_confirmacion")]
    public bool RequiereConfirmacion { get; set; }
}


// =============================================================
// TURNO
// =============================================================

public class TurnoAbierto
{
    [JsonPropertyName("id_turno")]
    public int IdTurno { get; set; }


    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } =
        string.Empty;


    [JsonPropertyName("id_caja")]
    public int IdCaja { get; set; }


    [JsonPropertyName("caja_codigo")]
    public string CajaCodigo { get; set; } =
        string.Empty;


    [JsonPropertyName("caja_nombre")]
    public string CajaNombre { get; set; } =
        string.Empty;


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


    [JsonPropertyName("estado")]
    public string Estado { get; set; } =
        string.Empty;


    [JsonPropertyName("fecha_apertura")]
    public string? FechaApertura { get; set; }
}