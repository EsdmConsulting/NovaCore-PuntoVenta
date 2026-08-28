using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Auth;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Auth;

public class AuthService
{
    public async Task<LoginResponse> LoginAsync(
        string usuario,
        string password)
    {
        try
        {
            var body = new
            {
                usuario,
                password
            };

            var response =
                await ApiClient.Http.PostAsJsonAsync(
                    "api/pos/auth/login",
                    body
                );

            var contenido =
                await response.Content.ReadAsStringAsync();

            // Por seguridad mientras terminamos de limpiar
            // todos los Notices del servidor.
            var inicioJson =
                contenido.IndexOf('{');

            if (inicioJson >= 0)
            {
                contenido =
                    contenido.Substring(inicioJson);
            }

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new LoginResponse
                {
                    Res = 0,
                    Msg = "El servidor no devolvió información."
                };
            }

            var resultado =
                JsonSerializer.Deserialize<LoginResponse>(
                    contenido,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (resultado is null)
            {
                return new LoginResponse
                {
                    Res = 0,
                    Msg = "El servidor devolvió una respuesta inválida."
                };
            }

            return resultado;
        }
        catch (HttpRequestException)
        {
            return new LoginResponse
            {
                Res = 0,
                Msg = "No fue posible conectarse con el servidor."
            };
        }
        catch (Exception ex)
        {
            return new LoginResponse
            {
                Res = 0,
                Msg = $"Ocurrió un error inesperado al iniciar sesión: {ex.Message}"
            };
        }
    }
}