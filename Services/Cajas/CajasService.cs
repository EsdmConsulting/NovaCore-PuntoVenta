using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Caja;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Cajas;

public class CajasService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };


    // =====================================================
    // OBTENER CAJAS
    // =====================================================

    public async Task<CajasResponse> ObtenerCajasAsync(
        int? idUnidadOperativa = null)
    {
        try
        {
            var endpoint = "api/pos/cajas";

            if (idUnidadOperativa.HasValue)
            {
                endpoint +=
                    $"?id_unidad_operativa={idUnidadOperativa.Value}";
            }

            var response =
                await ApiClient.Http.GetAsync(endpoint);

            var contenido =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine("====================================");
            Console.WriteLine("RESPUESTA API CAJAS:");
            Console.WriteLine(contenido);
            Console.WriteLine("====================================");

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new CajasResponse
                {
                    Res = 0,
                    Msg = "El servidor no devolvió información."
                };
            }

            // =====================================================
            // PARCHE TEMPORAL
            // =====================================================
            // El backend puede imprimir texto antes del JSON.
            // Buscamos el primer carácter '{' para quedarnos
            // únicamente con la respuesta JSON.
            // =====================================================

            contenido = LimpiarRespuestaPhp(contenido);

            try
            {
                var resultado =
                    JsonSerializer.Deserialize<CajasResponse>(
                        contenido,
                        JsonOptions);

                if (resultado is null)
                {
                    return new CajasResponse
                    {
                        Res = 0,
                        Msg = "La respuesta del servidor está vacía."
                    };
                }

                return resultado;
            }
            catch (JsonException ex)
            {
                Console.WriteLine("ERROR DESERIALIZANDO CAJAS:");
                Console.WriteLine(ex);

                return new CajasResponse
                {
                    Res = 0,
                    Msg = "La respuesta del servidor no contiene JSON válido."
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR CONSULTANDO CAJAS:");
            Console.WriteLine(ex);

            return new CajasResponse
            {
                Res = 0,
                Msg = $"No fue posible consultar las cajas: {ex.Message}"
            };
        }
    }


    // =====================================================
    // ASIGNAR CAJA
    // =====================================================
    //
    // Este método intenta reservar una caja para esta
    // terminal.
    //
    // Backend:
    //
    // PUT /api/pos/cajas/{id}/asignar
    //
    // El backend realiza la validación de manera atómica:
    //
    // en_uso = FALSE
    //          ↓
    // en_uso = TRUE
    //
    // Si otra computadora tomó la caja antes, el backend
    // rechazará la asignación.
    //
    // =====================================================

    public async Task<AsignarCajaResponse> AsignarCajaAsync(
        int idCaja)
    {
        try
        {
            if (idCaja <= 0)
            {
                return new AsignarCajaResponse
                {
                    Res = 0,
                    Msg = "El ID de la caja no es válido."
                };
            }

            var endpoint =
                $"api/pos/cajas/{idCaja}/asignar";

            // No necesitamos enviar información adicional.
            // El ID de la caja ya forma parte de la URL.
            using var contenidoRequest =
                new StringContent(
                    string.Empty,
                    System.Text.Encoding.UTF8,
                    "application/json");

            var response =
                await ApiClient.Http.PutAsync(
                    endpoint,
                    contenidoRequest);

            var contenido =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine("====================================");
            Console.WriteLine("RESPUESTA API ASIGNAR CAJA:");
            Console.WriteLine(contenido);
            Console.WriteLine("====================================");

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new AsignarCajaResponse
                {
                    Res = 0,
                    Msg = "El servidor no devolvió información al asignar la caja."
                };
            }

            contenido = LimpiarRespuestaPhp(contenido);

            try
            {
                var resultado =
                    JsonSerializer.Deserialize<AsignarCajaResponse>(
                        contenido,
                        JsonOptions);

                if (resultado is null)
                {
                    return new AsignarCajaResponse
                    {
                        Res = 0,
                        Msg = "La respuesta del servidor está vacía."
                    };
                }

                return resultado;
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    "ERROR DESERIALIZANDO ASIGNACIÓN DE CAJA:");

                Console.WriteLine(ex);

                return new AsignarCajaResponse
                {
                    Res = 0,
                    Msg = "La respuesta del servidor no contiene JSON válido."
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR ASIGNANDO CAJA:");
            Console.WriteLine(ex);

            return new AsignarCajaResponse
            {
                Res = 0,
                Msg = $"No fue posible asignar la caja: {ex.Message}"
            };
        }
    }


    // =====================================================
    // LIMPIAR RESPUESTA PHP
    // =====================================================
    //
    // Temporalmente algunos endpoints pueden imprimir
    // Notices/Warnings antes del JSON.
    //
    // Cuando el backend esté completamente limpio, esta
    // función podría eliminarse.
    //
    // =====================================================

    private static string LimpiarRespuestaPhp(
        string contenido)
    {
        var inicioJson =
            contenido.IndexOf('{');

        if (inicioJson >= 0)
        {
            return contenido.Substring(inicioJson);
        }

        return contenido;
    }
}