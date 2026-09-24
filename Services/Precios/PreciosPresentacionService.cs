using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Precios;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Precios;

public class PreciosPresentacionService
{
    public async Task<PrecioPresentacionResponse> ObtenerConfiguracionAsync(
        int idPresentacion,
        decimal? cantidad = null)
    {
        try
        {
            if (idPresentacion <= 0)
            {
                return new PrecioPresentacionResponse
                {
                    Res = 0,
                    Msg = "La presentación indicada no es válida."
                };
            }

            var url =
                $"api/pos/precios/presentacion?id_presentacion={idPresentacion}";

            if (cantidad.HasValue && cantidad.Value > 0)
            {
                var cantidadTexto =
                    cantidad.Value.ToString(
                        CultureInfo.InvariantCulture
                    );

                url += $"&cantidad={cantidadTexto}";
            }

            var response =
                await ApiClient.Http.GetAsync(url);

            var contenido =
                await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new PrecioPresentacionResponse
                {
                    Res = 0,
                    Msg = "El servidor no devolvió información."
                };
            }

            /*
             * Esto lo conservamos por compatibilidad.
             *
             * Si PHP llegara a mandar algún Warning / Notice
             * antes del JSON, buscamos el inicio real del objeto.
             */
            var inicioJson =
                contenido.IndexOf('{');

            if (inicioJson > 0)
            {
                contenido =
                    contenido.Substring(inicioJson);
            }

            var resultado =
                JsonSerializer.Deserialize<PrecioPresentacionResponse>(
                    contenido,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (resultado is null)
            {
                return new PrecioPresentacionResponse
                {
                    Res = 0,
                    Msg = "La respuesta del servidor es inválida."
                };
            }

            /*
             * Aunque PHP ya manda res = 0 cuando algo falla,
             * también respetamos el status HTTP por seguridad.
             */
            if (!response.IsSuccessStatusCode &&
                resultado.Res != 1)
            {
                if (string.IsNullOrWhiteSpace(resultado.Msg))
                {
                    resultado.Msg =
                        $"Error HTTP {(int)response.StatusCode}.";
                }

                return resultado;
            }

            return resultado;
        }
        catch (JsonException ex)
        {
            return new PrecioPresentacionResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible interpretar la configuración de precios: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new PrecioPresentacionResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible consultar la configuración de precios: {ex.Message}"
            };
        }
    }
}