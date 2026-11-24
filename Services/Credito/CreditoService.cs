using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using NovaCoreESDM.Models.Credito;
using NovaCoreESDM.Services.Api;
using NovaCoreESDM.Services.Offline;


namespace NovaCoreESDM.Services.Credito;


public class CreditoService
{
    private static readonly JsonSerializerOptions
        JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive =
                    true,

                NumberHandling =
                    JsonNumberHandling
                        .AllowReadingFromString
            };


    // =========================================================
    // CONSULTAR ESTADO DE CRÉDITO
    // =========================================================

    public async Task<EstadoCreditoResponse>
        ConsultarEstadoAsync(
            int idCliente,
            decimal montoNuevaVenta)
    {
        try
        {
            // =================================================
            // VALIDACIONES
            // =================================================

            if (idCliente <= 0)
            {
                return new EstadoCreditoResponse
                {
                    Res = 0,
                    Msg =
                        "Se requiere un cliente válido."
                };
            }


            if (montoNuevaVenta <= 0)
            {
                return new EstadoCreditoResponse
                {
                    Res = 0,
                    Msg =
                        "El importe de la venta no es válido."
                };
            }


            // Sin internet: con la copia de crédito de la caja.
            if (!ConexionService.EstaEnLinea)
                return EstadoSinConexion(idCliente, montoNuevaVenta);


            // =================================================
            // REQUEST
            // =================================================

            var request =
                new EstadoCreditoRequest
                {
                    IdCliente =
                        idCliente,

                    MontoNuevaVenta =
                        montoNuevaVenta
                };


            // =================================================
            // API
            // =================================================

            var response =
                await ApiClient.Http
                    .PostAsJsonAsync(
                        "api/comercial/credito/estado",
                        request
                    );


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            // =================================================
            // DEBUG
            // =================================================

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA ESTADO DE CRÉDITO:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            // =================================================
            // VALIDAR RESPUESTA
            // =================================================

            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new EstadoCreditoResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor no devolvió información de crédito."
                };
            }


            // =================================================
            // LIMPIAR POSIBLES WARNINGS PHP
            // =================================================

            var inicioJson =
                contenido.IndexOf('{');


            if (inicioJson < 0)
            {
                return new EstadoCreditoResponse
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


            // =================================================
            // DESERIALIZAR
            // =================================================

            var resultado =
                JsonSerializer
                    .Deserialize<EstadoCreditoResponse>(
                        contenido,
                        JsonOptions
                    );


            return resultado
                   ?? new EstadoCreditoResponse
                   {
                       Res = 0,

                       Msg =
                           "La respuesta del servicio de crédito es inválida."
                   };
        }
        catch (JsonException ex)
        {
            return new EstadoCreditoResponse
            {
                Res = 0,

                Msg =
                    $"La respuesta de crédito no tiene un formato válido: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            if (ConexionService.EsErrorDeRed(ex))
            {
                ConexionService.ReportarFalloDeRed();
                return EstadoSinConexion(idCliente, montoNuevaVenta);
            }

            return new EstadoCreditoResponse
            {
                Res = 0,

                Msg =
                    $"No fue posible consultar el crédito: {ex.Message}"
            };
        }
    }


    /// <summary>Estado del crédito con la copia local (ya descuenta lo vendido sin conexión).</summary>
    public static EstadoCreditoResponse EstadoSinConexion(int idCliente, decimal montoNuevaVenta)
    {
        var estado = CreditoLocal.Estado(idCliente, montoNuevaVenta);

        return estado is null
            ? new EstadoCreditoResponse
            {
                Res = 0,
                Msg = "Sin conexión: este cliente no está en la copia de crédito de la caja. Se necesita internet para venderle a crédito."
            }
            : new EstadoCreditoResponse
            {
                Res = 1,
                Msg = "Crédito consultado con la copia de la caja (sin conexión).",
                Data = estado
            };
    }
}
