using System;
using System.Text.Json;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Productos;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Productos;

public class ProductosService
{
    public async Task<CatalogoProductosResponse> ObtenerCatalogoAsync()
    {
        try
        {
            var response =
                await ApiClient.Http.GetAsync(
                    "api/consulta/catalogo_productos"
                );

            var contenido =
                await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new CatalogoProductosResponse
                {
                    Status = 0,
                    Message = "El servidor no devolvió información."
                };
            }

            var inicioJson = contenido.IndexOf('{');

            if (inicioJson >= 0)
            {
                contenido =
                    contenido.Substring(inicioJson);
            }

            var resultado =
                JsonSerializer.Deserialize<CatalogoProductosResponse>(
                    contenido,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return resultado ??
                   new CatalogoProductosResponse
                   {
                       Status = 0,
                       Message = "La respuesta del servidor es inválida."
                   };
        }
        catch (Exception ex)
        {
            return new CatalogoProductosResponse
            {
                Status = 0,
                Message =
                    $"No fue posible consultar los productos: {ex.Message}"
            };
        }
    }
}