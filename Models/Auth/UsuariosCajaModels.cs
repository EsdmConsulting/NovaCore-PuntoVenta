using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Auth;

// CHG-0003: GET api/pos/auth/usuarios?uuid_caja=...
// Usuarios que pueden entrar en la caja de este equipo.

public class UsuariosCajaResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public UsuariosCajaData? Data { get; set; }

    /// <summary>True cuando no se pudo llegar al servidor.</summary>
    [JsonIgnore]
    public bool SinConexion { get; set; }
}

public class UsuariosCajaData
{
    [JsonPropertyName("caja")]
    public CajaCatalogoUsuarios? Caja { get; set; }

    [JsonPropertyName("usuarios")]
    public List<UsuarioCatalogo> Usuarios { get; set; } = new();
}

public class CajaCatalogoUsuarios
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("id_unidad_operativa")]
    public int IdUnidadOperativa { get; set; }

    [JsonPropertyName("unidad_nombre")]
    public string UnidadNombre { get; set; } = string.Empty;
}

public class UsuarioCatalogo
{
    [JsonPropertyName("id_usuario")]
    public int IdUsuario { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("usuario")]
    public string Usuario { get; set; } = string.Empty;

    [JsonPropertyName("id_rol")]
    public int IdRol { get; set; }

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = string.Empty;

    [JsonPropertyName("es_administrador")]
    public bool EsAdministrador { get; set; }
}
