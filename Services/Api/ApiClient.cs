using System;
using System.Net;
using System.Net.Http;

namespace NovaCoreESDM.Services.Api;

public static class ApiClient
{
    private static readonly CookieContainer CookieContainer = new();

    private static readonly HttpClientHandler Handler = new()
    {
        CookieContainer = CookieContainer,
        UseCookies = true,

        // Respuestas comprimidas (el catálogo baja de 3 MB a ~200 KB).
        AutomaticDecompression = DecompressionMethods.All
    };

    public static HttpClient Http { get; } = new(Handler)
    {
        // Pruebas o producción: se elige al compilar (ver AmbienteServidor).
        BaseAddress = new Uri(
            AmbienteServidor.Url),

        // Antes 100 s (valor por defecto): con un internet "a medias" el
        // cajero se quedaba esperando. A los 30 s se pasa a modo sin conexión.
        Timeout = TimeSpan.FromSeconds(30)
    };
}