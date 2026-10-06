using System;
using System.Text.Json;
using System.Threading.Tasks;

using NovaCoreESDM.Models.Productos;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Productos;

public class ProductosService
{
    // =========================================================
    // OBTENER CATÁLOGO POS POR UNIDAD OPERATIVA
    // =========================================================

    public async Task<CatalogoProductosResponse>
        ObtenerCatalogoAsync(
            int idUnidadOperativa)
    {
        try
        {
            // =====================================================
            // VALIDAR UNIDAD OPERATIVA
            // =====================================================

            if (idUnidadOperativa <= 0)
            {
                return new CatalogoProductosResponse
                {
                    Status = 0,
                    Message =
                        "La unidad operativa no es válida."
                };
            }


            // =====================================================
            // ENDPOINT EXCLUSIVO DEL POS
            // =====================================================

            var endpoint =
                $"api/pos/productos/catalogo" +
                $"?id_unidad_operativa={idUnidadOperativa}";


            var response =
                await ApiClient.Http.GetAsync(
                    endpoint
                );


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            // =====================================================
            // DEBUG
            // =====================================================

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA API CATÁLOGO POS:");

            Console.WriteLine(
                $"UNIDAD OPERATIVA: {idUnidadOperativa}");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            // =====================================================
            // VALIDAR RESPUESTA
            // =====================================================

            if (string.IsNullOrWhiteSpace(
                contenido))
            {
                return new CatalogoProductosResponse
                {
                    Status = 0,
                    Message =
                        "El servidor no devolvió información."
                };
            }


            // =====================================================
            // LIMPIAR POSIBLES NOTICES PHP
            // =====================================================

            var inicioJson =
                contenido.IndexOf('{');


            if (inicioJson >= 0)
            {
                contenido =
                    contenido.Substring(
                        inicioJson);
            }


            // =====================================================
            // DESERIALIZAR
            // =====================================================

            var resultado =
                JsonSerializer
                    .Deserialize<CatalogoProductosResponse>(
                        contenido,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive =
                                true
                        });


            return resultado ??
                   new CatalogoProductosResponse
                   {
                       Status = 0,
                       Message =
                           "La respuesta del servidor es inválida."
                   };
        }
        catch (JsonException ex)
        {
            return new CatalogoProductosResponse
            {
                Status = 0,
                Message =
                    $"La respuesta del catálogo no tiene un formato válido: {ex.Message}"
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