using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Cancelaciones;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Cancelaciones;

public class CancelacionesService
{
    // =========================================================
    // CONFIGURACIÓN JSON
    // =========================================================

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,

            // PostgreSQL/PHP puede devolver algunos numeric
            // como string. Esto permite leer ambos casos.
            NumberHandling =
                JsonNumberHandling.AllowReadingFromString
        };


    // =========================================================
    // AUTORIZAR ADMINISTRADOR
    // POST /api/pos/cancelaciones/autorizar
    // =========================================================
    //
    // IMPORTANTE:
    //
    // Esta autorización NO sustituye la sesión normal del cajero.
    //
    // El backend crea:
    //
    // $_SESSION['cancelaciones_admin']
    //
    // y mantiene al cajero actualmente conectado.
    // =========================================================

    public async Task<AutorizarCancelacionResponse>
        AutorizarAdministradorAsync(
            string usuario,
            string password)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            return new AutorizarCancelacionResponse
            {
                Res = 0,
                Msg = "Debe ingresar el usuario del administrador."
            };
        }


        if (string.IsNullOrWhiteSpace(password))
        {
            return new AutorizarCancelacionResponse
            {
                Res = 0,
                Msg = "Debe ingresar la contraseña del administrador."
            };
        }


        try
        {
            var request =
                new AutorizarCancelacionRequest
                {
                    Usuario = usuario.Trim(),
                    Password = password
                };


            var response =
                await ApiClient.Http.PostAsJsonAsync(
                    "api/pos/cancelaciones/autorizar",
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
                "RESPUESTA API AUTORIZAR CANCELACIONES:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            // =================================================
            // LIMPIAR POSIBLES NOTICES PHP
            // =================================================

            contenido =
                LimpiarRespuestaPhp(
                    contenido
                );


            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new AutorizarCancelacionResponse
                {
                    Res = 0,
                    Msg =
                        "El servidor no devolvió información al autorizar al administrador."
                };
            }


            // =================================================
            // DESERIALIZAR
            // =================================================

            var resultado =
                JsonSerializer.Deserialize<
                    AutorizarCancelacionResponse
                >(
                    contenido,
                    JsonOptions
                );


            return resultado ??
                   new AutorizarCancelacionResponse
                   {
                       Res = 0,
                       Msg =
                           "El servidor devolvió una respuesta inválida al autorizar al administrador."
                   };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR JSON AUTORIZAR CANCELACIONES:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new AutorizarCancelacionResponse
            {
                Res = 0,
                Msg =
                    $"La respuesta de autorización no tiene un formato válido: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR AL AUTORIZAR CANCELACIONES:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new AutorizarCancelacionResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible autorizar al administrador: {ex.Message}"
            };
        }
    }


    // =========================================================
    // OBTENER VENTAS PARA CANCELACIÓN
    // GET /api/pos/cancelaciones
    // =========================================================
    //
    // Filtros:
    //
    // - fecha
    // - folio
    // - estado
    //
    // El backend únicamente devuelve ventas FINALIZADAS
    // o CANCELADAS.
    // =========================================================

    public async Task<CancelacionesListResponse>
        ObtenerVentasAsync(
            DateTime fecha,
            string? folio = null,
            string? estado = null)
    {
        try
        {
            var parametros =
                new List<string>();


            // =================================================
            // FECHA
            // =================================================

            parametros.Add(
                "fecha=" +
                Uri.EscapeDataString(
                    fecha.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    )
                )
            );


            // =================================================
            // FOLIO
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                    folio))
            {
                parametros.Add(
                    "folio=" +
                    Uri.EscapeDataString(
                        folio.Trim()
                    )
                );
            }


            // =================================================
            // ESTADO
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                    estado))
            {
                parametros.Add(
                    "estado=" +
                    Uri.EscapeDataString(
                        estado.Trim()
                            .ToUpperInvariant()
                    )
                );
            }


            // =================================================
            // ENDPOINT
            // =================================================

            var endpoint =
                "api/pos/cancelaciones";


            if (parametros.Count > 0)
            {
                endpoint +=
                    "?" +
                    string.Join(
                        "&",
                        parametros
                    );
            }


            // =================================================
            // REQUEST
            // =================================================

            var response =
                await ApiClient.Http.GetAsync(
                    endpoint
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
                "RESPUESTA API LISTAR CANCELACIONES:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                $"ENDPOINT: {endpoint}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            // =================================================
            // LIMPIAR POSIBLES NOTICES PHP
            // =================================================

            contenido =
                LimpiarRespuestaPhp(
                    contenido
                );


            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new CancelacionesListResponse
                {
                    Res = 0,
                    Msg =
                        "El servidor no devolvió información al consultar las ventas."
                };
            }


            // =================================================
            // DESERIALIZAR
            // =================================================

            var resultado =
                JsonSerializer.Deserialize<
                    CancelacionesListResponse
                >(
                    contenido,
                    JsonOptions
                );


            return resultado ??
                   new CancelacionesListResponse
                   {
                       Res = 0,
                       Msg =
                           "El servidor devolvió una respuesta inválida al consultar las ventas."
                   };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR JSON LISTAR CANCELACIONES:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new CancelacionesListResponse
            {
                Res = 0,
                Msg =
                    $"La respuesta del listado de cancelaciones no tiene un formato válido: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR AL LISTAR CANCELACIONES:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new CancelacionesListResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible consultar las ventas: {ex.Message}"
            };
        }
    }


    // =========================================================
    // OBTENER DETALLE DE UNA VENTA
    // GET /api/pos/cancelaciones/{id}
    // =========================================================

    public async Task<CancelacionDetalleResponse>
        ObtenerDetalleAsync(
            int idVenta)
    {
        if (idVenta <= 0)
        {
            return new CancelacionDetalleResponse
            {
                Res = 0,
                Msg =
                    "El id de la venta no es válido."
            };
        }


        try
        {
            var endpoint =
                $"api/pos/cancelaciones/{idVenta}";


            var response =
                await ApiClient.Http.GetAsync(
                    endpoint
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
                "RESPUESTA API DETALLE CANCELACIÓN:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                $"ENDPOINT: {endpoint}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            // =================================================
            // LIMPIAR POSIBLES NOTICES PHP
            // =================================================

            contenido =
                LimpiarRespuestaPhp(
                    contenido
                );


            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new CancelacionDetalleResponse
                {
                    Res = 0,
                    Msg =
                        "El servidor no devolvió información del detalle de la venta."
                };
            }


            // =================================================
            // DESERIALIZAR
            // =================================================

            var resultado =
                JsonSerializer.Deserialize<
                    CancelacionDetalleResponse
                >(
                    contenido,
                    JsonOptions
                );


            return resultado ??
                   new CancelacionDetalleResponse
                   {
                       Res = 0,
                       Msg =
                           "El servidor devolvió una respuesta inválida al consultar el detalle."
                   };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR JSON DETALLE CANCELACIÓN:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new CancelacionDetalleResponse
            {
                Res = 0,
                Msg =
                    $"La respuesta del detalle de la venta no tiene un formato válido: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR AL CONSULTAR DETALLE CANCELACIÓN:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new CancelacionDetalleResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible consultar el detalle de la venta: {ex.Message}"
            };
        }
    }


    // =========================================================
    // CANCELAR VENTA
    // POST /api/pos/cancelaciones/{id}/cancelar
    // =========================================================
    //
    // El backend se encarga de:
    //
    // - Validar autorización administrativa.
    // - Validar que la venta esté FINALIZADA.
    // - Evitar doble cancelación.
    // - Restaurar inventario.
    // - Cambiar estado a CANCELADA.
    // - Guardar auditoría.
    //
    // El frontend únicamente manda:
    //
    // {
    //     "motivo": "..."
    // }
    // =========================================================

    public async Task<CancelarVentaResponse>
        CancelarVentaAsync(
            int idVenta,
            string motivo)
    {
        if (idVenta <= 0)
        {
            return new CancelarVentaResponse
            {
                Res = 0,
                Msg =
                    "El id de la venta no es válido."
            };
        }


        if (string.IsNullOrWhiteSpace(
                motivo))
        {
            return new CancelarVentaResponse
            {
                Res = 0,
                Msg =
                    "Debe indicar el motivo de la cancelación."
            };
        }


        motivo =
            motivo.Trim();


        if (motivo.Length > 500)
        {
            return new CancelarVentaResponse
            {
                Res = 0,
                Msg =
                    "El motivo de cancelación no puede superar los 500 caracteres."
            };
        }


        try
        {
            var request =
                new CancelarVentaRequest
                {
                    Motivo =
                        motivo
                };


            var endpoint =
                $"api/pos/cancelaciones/{idVenta}/cancelar";


            var response =
                await ApiClient.Http.PostAsJsonAsync(
                    endpoint,
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
                "RESPUESTA API CANCELAR VENTA:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                $"ENDPOINT: {endpoint}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            // =================================================
            // LIMPIAR POSIBLES NOTICES PHP
            // =================================================

            contenido =
                LimpiarRespuestaPhp(
                    contenido
                );


            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new CancelarVentaResponse
                {
                    Res = 0,
                    Msg =
                        "El servidor no devolvió información al cancelar la venta."
                };
            }


            // =================================================
            // DESERIALIZAR
            // =================================================

            var resultado =
                JsonSerializer.Deserialize<
                    CancelarVentaResponse
                >(
                    contenido,
                    JsonOptions
                );


            return resultado ??
                   new CancelarVentaResponse
                   {
                       Res = 0,
                       Msg =
                           "El servidor devolvió una respuesta inválida al cancelar la venta."
                   };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR JSON CANCELAR VENTA:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new CancelarVentaResponse
            {
                Res = 0,
                Msg =
                    $"La respuesta de cancelación no tiene un formato válido: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR AL CANCELAR VENTA:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new CancelarVentaResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible cancelar la venta: {ex.Message}"
            };
        }
    }
    
    
    public async Task<bool> CerrarAutorizacionAsync()
    {
        try
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "api/pos/cancelaciones/cerrar-autorizacion"
                );

            using var response =
                await ApiClient.Http.SendAsync(request);

            var contenido =
                await response.Content.ReadAsStringAsync();

            contenido =
                LimpiarRespuestaPhp(contenido);

            Console.WriteLine("====================================");
            Console.WriteLine("RESPUESTA API CERRAR AUTORIZACIÓN CANCELACIONES:");
            Console.WriteLine($"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine(contenido);
            Console.WriteLine("====================================");

            if (!response.IsSuccessStatusCode)
                return false;

            var resultado =
                JsonSerializer.Deserialize<CerrarAutorizacionResponse>(
                    contenido,
                    JsonOptions
                );

            return resultado?.Res == 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("====================================");
            Console.WriteLine("ERROR CERRAR AUTORIZACIÓN CANCELACIONES:");
            Console.WriteLine(ex);
            Console.WriteLine("====================================");

            return false;
        }
    }


    // =========================================================
    // LIMPIAR RESPUESTA PHP
    // =========================================================
    //
    // Conservamos la misma idea utilizada por VentasService.
    //
    // Si PHP imprime accidentalmente un Notice/Warning antes
    // del JSON, buscamos el primer "{" y deserializamos desde ahí.
    // =========================================================

    private static string LimpiarRespuestaPhp(
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(
                contenido))
        {
            return contenido;
        }


        var inicioJson =
            contenido.IndexOf('{');


        return inicioJson >= 0
            ? contenido.Substring(
                inicioJson)
            : contenido;
    }
    
    
    
}