using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Turno;

public class AbrirTurnoResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public TurnoAbierto? Data { get; set; }
}

public class TurnoAbierto
{
    // =========================================================
    // APERTURA NORMAL
    // =========================================================

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("id_caja")]
    public int IdCaja { get; set; }

    [JsonPropertyName("id_usuario_apertura")]
    public int IdUsuarioApertura { get; set; }

    [JsonPropertyName("fondo_inicial")]
    public decimal FondoInicial { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("fecha_apertura")]
    public string? FechaApertura { get; set; }


    // =========================================================
    // CUANDO YA EXISTE UN TURNO ABIERTO
    // =========================================================

    /*
     * El backend devuelve:
     *
     * data: {
     *     "id_turno": 10
     * }
     */
    [JsonPropertyName("id_turno")]
    public int IdTurnoExistente { get; set; }


    // =========================================================
    // ID EFECTIVO DEL TURNO
    // =========================================================

    /*
     * Si el turno acaba de ser creado:
     *      Id contiene el valor.
     *
     * Si el turno ya existía:
     *      IdTurnoExistente contiene el valor.
     */
    [JsonIgnore]
    public int IdTurno =>
        Id > 0
            ? Id
            : IdTurnoExistente;
}