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
        var body =
            new
            {
                usuario,
                password
            };


        var response =
            await ApiClient.Http
                .PostAsJsonAsync(
                    "api/pos/auth/login",
                    body
                );


        var contenido =
            await response.Content
                .ReadAsStringAsync();


        // =====================================================
        // DEBUG RESPUESTA REAL DEL SERVIDOR
        // =====================================================

        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "RESPUESTA LOGIN:");

        Console.WriteLine(
            $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

        Console.WriteLine(
            contenido);

        Console.WriteLine(
            "====================================");


        // =====================================================
        // RESPUESTA VACÍA
        // =====================================================

        if (string.IsNullOrWhiteSpace(
                contenido))
        {
            return new LoginResponse
            {
                Res = 0,
                Msg =
                    "El servidor no devolvió información."
            };
        }


        // =====================================================
        // BUSCAR INICIO DEL JSON
        // =====================================================

        var inicioJson =
            contenido.IndexOf('{');


        /*
         * Si NO existe un {
         *
         * entonces el servidor NO nos devolvió JSON.
         *
         * Probablemente:
         *
         * - HTML
         * - 404
         * - 500
         * - página de Apache
         * - error PHP
         */

        if (inicioJson < 0)
        {
            return new LoginResponse
            {
                Res = 0,
                Msg =
                    $"El servidor devolvió una respuesta inválida. HTTP {(int)response.StatusCode}."
            };
        }


        contenido =
            contenido.Substring(
                inicioJson
            );


        // =====================================================
        // DESERIALIZAR
        // =====================================================

        var resultado =
            JsonSerializer.Deserialize<LoginResponse>(
                contenido,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive =
                        true
                }
            );


        if (resultado is null)
        {
            return new LoginResponse
            {
                Res = 0,
                Msg =
                    "El servidor devolvió una respuesta inválida."
            };
        }


        return resultado;
    }
    catch (JsonException ex)
    {
        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "ERROR JSON LOGIN:");

        Console.WriteLine(
            ex);

        Console.WriteLine(
            "====================================");


        return new LoginResponse
        {
            Res = 0,
            Msg =
                $"La respuesta del servidor no tiene un formato válido: {ex.Message}"
        };
    }
    catch (HttpRequestException)
    {
        return new LoginResponse
        {
            Res = 0,
            Msg =
                "No fue posible conectarse con el servidor."
        };
    }
    catch (Exception ex)
    {
        return new LoginResponse
        {
            Res = 0,
            Msg =
                $"Ocurrió un error inesperado al iniciar sesión: {ex.Message}"
        };
    }
}
}