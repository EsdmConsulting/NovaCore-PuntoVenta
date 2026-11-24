using System;
using System.Text.Json;
using System.Threading.Tasks;

using NovaCoreESDM.Models.Productos;
using NovaCoreESDM.Services.Api;
using NovaCoreESDM.Services.Offline;

namespace NovaCoreESDM.Services.Productos;

public class ProductosService
{
    // Se reutilizan (crearlas en cada carga es lento).
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
                $"{contenido.Length:N0} caracteres");

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

            // El catálogo pesa varios MB: se procesa fuera de la pantalla para
            // que no se congele en computadoras modestas.
            var json = contenido;

            var resultado =
                await Task.Run(() =>
                    JsonSerializer.Deserialize<CatalogoProductosResponse>(json, OpcionesJson));


            if (resultado is { Status: 1 })
            {
                // Copia para poder seguir vendiendo si se va el internet (en segundo plano).
                _ = Task.Run(() => CatalogoLocal.Guardar(idUnidadOperativa, json));
            }

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
            if (ConexionService.EsErrorDeRed(ex))
            {
                ConexionService.ReportarFalloDeRed();

                var copia = await Task.Run(() => CatalogoLocal.Obtener(idUnidadOperativa));
                if (copia is not null)
                    return copia;

                return new CatalogoProductosResponse
                {
                    Status = 0,
                    Message =
                        "No hay internet y esta caja todavía no tiene una copia del catálogo. Conéctate una vez para descargarlo."
                };
            }

            return new CatalogoProductosResponse
            {
                Status = 0,
                Message =
                    $"No fue posible consultar los productos: {ex.Message}"
            };
        }
    }
    
}