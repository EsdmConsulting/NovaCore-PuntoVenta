using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Turno;

public class ActividadTurnoResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public ActividadTurnoData? Data { get; set; }
}

public class ActividadTurnoData
{
    [JsonPropertyName("turno")]
    public ActividadTurnoInfo? Turno { get; set; }

    [JsonPropertyName("actividad")]
    public List<ActividadTurnoItem> Actividad { get; set; } = new();
}

public class ActividadTurnoInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

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
}

public class ActividadTurnoItem
{
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("naturaleza")]
    public string Naturaleza { get; set; } = string.Empty;

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("importe")]
    public decimal Importe { get; set; }

    [JsonPropertyName("referencia")]
    public string? Referencia { get; set; }

    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("id_origen")]
    public long? IdOrigen { get; set; }

    [JsonPropertyName("tipo_origen")]
    public string TipoOrigen { get; set; } = string.Empty;
}