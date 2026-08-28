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
        UseCookies = true
    };

    public static HttpClient Http { get; } = new(Handler)
    {
        BaseAddress = new Uri(
            "https://nubeesdm8.ddns.net:4433/Plataforma_LuisFer/")
    };
}