using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Auth;

public class LoginResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public LoginData? Data { get; set; }
}

public class LoginData
{
    [JsonPropertyName("id_usuario")]
    public int IdUsuario { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("usuario")]
    public string Usuario { get; set; } = string.Empty;

    [JsonPropertyName("foto")]
    public string? Foto { get; set; }

    [JsonPropertyName("id_rol")]
    public int IdRol { get; set; }

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = string.Empty;

    [JsonPropertyName("es_administrador")]
    public bool EsAdministrador { get; set; }

    [JsonPropertyName("es_cajero")]
    public bool EsCajero { get; set; }
}