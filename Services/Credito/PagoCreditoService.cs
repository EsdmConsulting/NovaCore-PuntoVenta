using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using NovaCoreESDM.Models.Credito;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Credito;

public class PagoCreditoService
{
    // =========================================================
    // OPCIONES JSON
    // =========================================================

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,

            NumberHandling =
                JsonNumberHandling
                    .AllowReadingFromString
        };


    // =========================================================
    // OBTENER RESUMEN DEL CLIENTE
    // =========================================================

    public async Task<ResumenCreditoResponse>
        ObtenerResumenClienteAsync(
            int idCliente)
    {
        try
        {
            if (idCliente <= 0)
            {
                return new ResumenCreditoResponse
                {
                    Res = 0,
                    Msg = "El cliente no es válido."
                };
            }


            var response =
                await ApiClient.Http
                    .GetAsync(
                        $"api/comercial/credito/resumen-cliente?id_cliente={idCliente}"
                    );


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA RESUMEN DE CRÉDITO:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            contenido =
                LimpiarRespuestaPhp(
                    contenido
                );


            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new ResumenCreditoResponse
                {
                    Res = 0,
                    Msg = "El servidor no devolvió información del crédito."
                };
            }


            var resultado =
                JsonSerializer
                    .Deserialize<ResumenCreditoResponse>(
                        contenido,
                        JsonOptions
                    );


            return resultado
                   ?? new ResumenCreditoResponse
                   {
                       Res = 0,
                       Msg = "El servidor devolvió una respuesta inválida."
                   };
        }
        catch (HttpRequestException)
        {
            return new ResumenCreditoResponse
            {
                Res = 0,
                Msg = "No fue posible conectarse con el servidor."
            };
        }
        catch (Exception ex)
        {
            return new ResumenCreditoResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible consultar el crédito: {ex.Message}"
            };
        }
    }


    // =========================================================
    // REGISTRAR ABONO
    // =========================================================

    public async Task<RegistrarAbonoCreditoResponse>
        RegistrarAbonoAsync(
            RegistrarAbonoCreditoRequest request)
    {
        try
        {
            if (request is null)
            {
                return new RegistrarAbonoCreditoResponse
                {
                    Res = 0,
                    Msg = "No existe información para registrar el abono."
                };
            }


            var response =
                await ApiClient.Http
                    .PostAsJsonAsync(
                        "api/comercial/credito/registrar-abono",
                        request,
                        JsonOptions
                    );


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA REGISTRAR ABONO DE CRÉDITO:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            contenido =
                LimpiarRespuestaPhp(
                    contenido
                );


            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new RegistrarAbonoCreditoResponse
                {
                    Res = 0,
                    Msg = "El servidor no devolvió información del abono."
                };
            }


            var resultado =
                JsonSerializer
                    .Deserialize<RegistrarAbonoCreditoResponse>(
                        contenido,
                        JsonOptions
                    );


            return resultado
                   ?? new RegistrarAbonoCreditoResponse
                   {
                       Res = 0,
                       Msg = "El servidor devolvió una respuesta inválida."
                   };
        }
        catch (HttpRequestException)
        {
            return new RegistrarAbonoCreditoResponse
            {
                Res = 0,
                Msg = "No fue posible conectarse con el servidor."
            };
        }
        catch (Exception ex)
        {
            return new RegistrarAbonoCreditoResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible registrar el abono: {ex.Message}"
            };
        }
    }


    // =========================================================
    // LIMPIAR RESPUESTA PHP
    // =========================================================
    //
    // Algunos endpoints pueden traer Notices o Warning
    // antes del JSON.
    //
    // Buscamos el primer {
    // y trabajamos desde ahí.
    //
    // =========================================================

    private static string LimpiarRespuestaPhp(
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(
                contenido))
        {
            return string.Empty;
        }


        var inicioJson =
            contenido.IndexOf(
                '{'
            );


        if (inicioJson >= 0)
        {
            contenido =
                contenido.Substring(
                    inicioJson
                );
        }


        return contenido.Trim();
    }
}