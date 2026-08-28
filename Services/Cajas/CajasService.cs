using System;
using System.Text.Json;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Caja;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Cajas;

public class CajasService
{
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
            // El backend actualmente imprime un Notice de PHP antes
            // del JSON debido a un session_start() duplicado.
            //
            // Cuando se corrija el backend, esto debe eliminarse.
            // =====================================================

            var inicioJson = contenido.IndexOf('{');

            if (inicioJson >= 0)
            {
                contenido = contenido.Substring(inicioJson);
            }

            try
            {
                var resultado =
                    JsonSerializer.Deserialize<CajasResponse>(
                        contenido,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

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
}


// MODIFICAR SERVICIO Y MODIFICAR ESTE CODIGO