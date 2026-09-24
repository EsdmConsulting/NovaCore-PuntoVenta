using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Ventas;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Ventas;

public class VentasService
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
    // OBTENER VENTA ACTUAL DEL TURNO
    // =========================================================

    public async Task<VentaActualResponse>
        ObtenerVentaActualAsync(
            int idTurno)
    {
        try
        {
            var endpoint =
                $"api/pos/ventas/actual?id_turno={idTurno}";


            var response =
                await ApiClient.Http.GetAsync(
                    endpoint);


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            // =================================================
            // DEBUG
            // =================================================

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA API VENTA ACTUAL:");

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
                    contenido);


            if (string.IsNullOrWhiteSpace(
                contenido))
            {
                return new VentaActualResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor no devolvió información de la venta actual."
                };
            }


            // =================================================
            // DESERIALIZAR
            // =================================================

            var resultado =
                JsonSerializer
                    .Deserialize<VentaActualResponse>(
                        contenido,
                        JsonOptions);


            return resultado ??
                   new VentaActualResponse
                   {
                       Res = 0,

                       Msg =
                           "El servidor devolvió una respuesta inválida."
                   };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR JSON VENTA ACTUAL:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new VentaActualResponse
            {
                Res = 0,

                Msg =
                    $"La respuesta de la venta actual no tiene un formato válido: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR AL CONSULTAR VENTA ACTUAL:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new VentaActualResponse
            {
                Res = 0,

                Msg =
                    $"No fue posible consultar la venta actual: {ex.Message}"
            };
        }
    }


    // =========================================================
    // CREAR VENTA
    // =========================================================

    public async Task<CrearVentaResponse>
        CrearVentaAsync(
            CrearVentaRequest request)
    {
        try
        {
            var response =
                await ApiClient.Http.PostAsJsonAsync(
                    "api/pos/ventas",
                    request);


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            // =================================================
            // DEBUG
            // =================================================

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA API CREAR VENTA:");

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
                    contenido);


            if (string.IsNullOrWhiteSpace(
                contenido))
            {
                return new CrearVentaResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor no devolvió información al crear la venta."
                };
            }


            // =================================================
            // DESERIALIZAR
            // =================================================

            var resultado =
                JsonSerializer
                    .Deserialize<CrearVentaResponse>(
                        contenido,
                        JsonOptions);


            return resultado ??
                   new CrearVentaResponse
                   {
                       Res = 0,

                       Msg =
                           "El servidor devolvió una respuesta inválida."
                   };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR JSON AL CREAR VENTA:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new CrearVentaResponse
            {
                Res = 0,

                Msg =
                    $"La respuesta al crear la venta no tiene un formato válido: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR AL CREAR VENTA:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new CrearVentaResponse
            {
                Res = 0,

                Msg =
                    $"No fue posible crear la venta: {ex.Message}"
            };
        }
    }
    
    
    
    // =========================================================
// ACTUALIZAR DATOS GENERALES DE LA VENTA
// =========================================================

public async Task<ActualizarVentaResponse>
    ActualizarVentaAsync(
        int idVenta,
        ActualizarVentaRequest request)
{
    try
    {
        var response =
            await ApiClient.Http
                .PutAsJsonAsync(
                    $"api/pos/ventas/{idVenta}",
                    request
                );


        var contenido =
            await response.Content
                .ReadAsStringAsync();


        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "RESPUESTA API ACTUALIZAR VENTA:");

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
            return new ActualizarVentaResponse
            {
                Res = 0,
                Msg =
                    "El servidor no devolvió información al actualizar la venta."
            };
        }


        var resultado =
            JsonSerializer
                .Deserialize<ActualizarVentaResponse>(
                    contenido,
                    JsonOptions
                );


        return resultado
               ?? new ActualizarVentaResponse
               {
                   Res = 0,
                   Msg =
                       "El servidor devolvió una respuesta inválida al actualizar la venta."
               };
    }
    catch (JsonException ex)
    {
        return new ActualizarVentaResponse
        {
            Res = 0,
            Msg =
                $"La respuesta al actualizar la venta no tiene un formato válido: {ex.Message}"
        };
    }
    catch (Exception ex)
    {
        return new ActualizarVentaResponse
        {
            Res = 0,
            Msg =
                $"No fue posible actualizar la venta: {ex.Message}"
        };
    }
}




    // =========================================================
    // AGREGAR PRODUCTO A LA VENTA
    // =========================================================

    public async Task<AgregarProductoVentaResponse>
        AgregarProductoAsync(
            int idVenta,
            AgregarProductoVentaRequest request)
    {
        try
        {
            var response =
                await ApiClient.Http.PostAsJsonAsync(
                    $"api/pos/ventas/{idVenta}/productos",
                    request);


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            // =================================================
            // DEBUG
            // =================================================

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA API AGREGAR PRODUCTO:");

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
                    contenido);


            if (string.IsNullOrWhiteSpace(
                contenido))
            {
                return new AgregarProductoVentaResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor no devolvió información al agregar el producto."
                };
            }


            // =================================================
            // DESERIALIZAR
            // =================================================

            var resultado =
                JsonSerializer
                    .Deserialize<AgregarProductoVentaResponse>(
                        contenido,
                        JsonOptions);


            return resultado ??
                   new AgregarProductoVentaResponse
                   {
                       Res = 0,

                       Msg =
                           "El servidor devolvió una respuesta inválida."
                   };
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR JSON AL AGREGAR PRODUCTO:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new AgregarProductoVentaResponse
            {
                Res = 0,

                Msg =
                    $"La respuesta al agregar el producto no tiene un formato válido: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR AL AGREGAR PRODUCTO:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new AgregarProductoVentaResponse
            {
                Res = 0,

                Msg =
                    $"No fue posible agregar el producto: {ex.Message}"
            };
        }
    }
    
    
    // =========================================================
    // ELIMINAR PRODUCTO DEL CARRITO
    // =========================================================
    
    public async Task<EliminarProductoVentaResponse>
        EliminarProductoAsync(
            int idVenta,
            int idDetalle)
    {
        try
        {
            var response =
                await ApiClient.Http.DeleteAsync(
                    $"api/pos/ventas/{idVenta}/productos/{idDetalle}"
                );

            var contenido =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA API ELIMINAR PRODUCTO:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            contenido =
                LimpiarRespuestaPhp(
                    contenido);


            var resultado =
                JsonSerializer.Deserialize<EliminarProductoVentaResponse>(
                    contenido,
                    JsonOptions);


            return resultado ??
                   new EliminarProductoVentaResponse
                   {
                       Res = 0,
                       Msg =
                           "El servidor devolvió una respuesta inválida."
                   };
        }
        catch (Exception ex)
        {
            return new EliminarProductoVentaResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible eliminar el producto: {ex.Message}"
            };
        }
    }
    
    
// =========================================================
// ACTUALIZAR DETALLE DE PRODUCTO
// =========================================================
//
// Este método es el principal.
//
// Permite actualizar:
//
// - Cantidad
// - Precio automático
// - Tipo de precio manual
// - Regresar a automático
//
// El backend es quien resuelve siempre el precio final.
// =========================================================

public async Task<ActualizarCantidadVentaResponse>
    ActualizarDetalleProductoAsync(
        int idVenta,
        int idDetalle,
        ActualizarCantidadVentaRequest request)
{
    try
    {
        var response =
            await ApiClient.Http.PutAsJsonAsync(
                $"api/pos/ventas/{idVenta}/productos/{idDetalle}",
                request);

        var contenido =
            await response.Content
                .ReadAsStringAsync();


        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "RESPUESTA API ACTUALIZAR DETALLE:");

        Console.WriteLine(
            $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

        Console.WriteLine(
            contenido);

        Console.WriteLine(
            "====================================");


        contenido =
            LimpiarRespuestaPhp(
                contenido);


        if (string.IsNullOrWhiteSpace(
                contenido))
        {
            return new ActualizarCantidadVentaResponse
            {
                Res = 0,
                Msg =
                    "El servidor no devolvió información al actualizar el producto."
            };
        }


        var resultado =
            JsonSerializer.Deserialize<ActualizarCantidadVentaResponse>(
                contenido,
                JsonOptions);


        return resultado ??
               new ActualizarCantidadVentaResponse
               {
                   Res = 0,
                   Msg =
                       "El servidor devolvió una respuesta inválida."
               };
    }
    catch (JsonException ex)
    {
        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "ERROR JSON AL ACTUALIZAR DETALLE:");

        Console.WriteLine(ex);

        Console.WriteLine(
            "====================================");


        return new ActualizarCantidadVentaResponse
        {
            Res = 0,
            Msg =
                $"La respuesta al actualizar el producto no tiene un formato válido: {ex.Message}"
        };
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "ERROR AL ACTUALIZAR DETALLE:");

        Console.WriteLine(ex);

        Console.WriteLine(
            "====================================");


        return new ActualizarCantidadVentaResponse
        {
            Res = 0,
            Msg =
                $"No fue posible actualizar el producto: {ex.Message}"
        };
    }
}


// =========================================================
// ACTUALIZAR ÚNICAMENTE CANTIDAD
// =========================================================
//
// Conservamos este método para no romper el código actual
// del ViewModel.
//
// Internamente utiliza el método nuevo.
// =========================================================

public async Task<ActualizarCantidadVentaResponse>
    ActualizarCantidadProductoAsync(
        int idVenta,
        int idDetalle,
        decimal cantidadComercial)
{
    var request =
        new ActualizarCantidadVentaRequest
        {
            CantidadComercial =
                cantidadComercial,

            /*
             * No mandamos PrecioAutomatico.
             *
             * Al ir como null, el backend conserva la
             * configuración actual de precio de la línea.
             *
             * Ejemplo:
             *
             * Si la línea está en automático:
             *     seguirá automática.
             *
             * Si está forzada a MAYOREO:
             *     seguirá forzada a MAYOREO.
             */
            PrecioAutomatico = null
        };


    return await ActualizarDetalleProductoAsync(
        idVenta,
        idDetalle,
        request);
}


// =========================================================
// ESTABLECER PRECIO MANUAL
// =========================================================
//
// Ejemplo:
//
// cantidad = 3
//
// Cajero selecciona:
// MAYOREO
//
// Se envía:
//
// precio_automatico = false
// tipo_precio_maximo = MAYOREO
//
// El backend valida que MAYOREO realmente exista como
// nivel disponible para esa presentación.
// =========================================================

public async Task<ActualizarCantidadVentaResponse>
    EstablecerPrecioManualAsync(
        int idVenta,
        int idDetalle,
        string tipoPrecio)
{
    if (string.IsNullOrWhiteSpace(
            tipoPrecio))
    {
        return new ActualizarCantidadVentaResponse
        {
            Res = 0,
            Msg =
                "Debe seleccionar un tipo de precio."
        };
    }


    var request =
        new ActualizarCantidadVentaRequest
        {
            PrecioAutomatico = false,

            TipoPrecioMaximo =
                tipoPrecio.Trim()
        };


    return await ActualizarDetalleProductoAsync(
        idVenta,
        idDetalle,
        request);
}


// =========================================================
// REGRESAR A PRECIO AUTOMÁTICO
// =========================================================
//
// MUY IMPORTANTE:
//
// Aquí necesitamos mandar:
//
// precio_automatico = true
// tipo_precio_maximo = null
//
// Ese null debe viajar en el JSON para quitar cualquier
// precio manual o límite previamente guardado.
// =========================================================

public async Task<ActualizarCantidadVentaResponse>
    EstablecerPrecioAutomaticoAsync(
        int idVenta,
        int idDetalle)
{
    var request =
        new ActualizarCantidadVentaRequest
        {
            PrecioAutomatico = true,
            TipoPrecioMaximo = null
        };


    return await ActualizarDetalleProductoAsync(
        idVenta,
        idDetalle,
        request);
}


// =========================================================
// CANCELAR CARRITO COMPLETO
// =========================================================

public async Task<CancelarVentaResponse>
    CancelarVentaAsync(
        int idVenta)
{
    try
    {
        var response =
            await ApiClient.Http.PostAsJsonAsync(
                $"api/pos/ventas/{idVenta}/cancelar",
                new
                {
                    motivo =
                        "Carrito cancelado por el cajero"
                });

        var contenido =
            await response.Content
                .ReadAsStringAsync();


        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "RESPUESTA API CANCELAR VENTA:");

        Console.WriteLine(
            $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

        Console.WriteLine(
            contenido);

        Console.WriteLine(
            "====================================");


        contenido =
            LimpiarRespuestaPhp(
                contenido);


        if (string.IsNullOrWhiteSpace(contenido))
        {
            return new CancelarVentaResponse
            {
                Res = 0,
                Msg =
                    "El servidor no devolvió información al cancelar el carrito."
            };
        }


        var resultado =
            JsonSerializer.Deserialize<CancelarVentaResponse>(
                contenido,
                JsonOptions);


        return resultado ??
               new CancelarVentaResponse
               {
                   Res = 0,
                   Msg =
                       "El servidor devolvió una respuesta inválida."
               };
    }
    catch (Exception ex)
    {
        return new CancelarVentaResponse
        {
            Res = 0,
            Msg =
                $"No fue posible cancelar el carrito: {ex.Message}"
        };
    }
}



// =========================================================
// REGUSTRAR VENTA
// =========================================================


public async Task<RegistrarPagoResponse>
    RegistrarPagoAsync(
        int idVenta,
        RegistrarPagoRequest request)
{
    try
    {
        var response =
            await ApiClient.Http
                .PostAsJsonAsync(
                    $"api/pos/ventas/{idVenta}/pagos",
                    request);

        var contenido =
            await response.Content
                .ReadAsStringAsync();


        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "RESPUESTA API REGISTRAR PAGO:");

        Console.WriteLine(
            $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

        Console.WriteLine(
            contenido);

        Console.WriteLine(
            "====================================");


        contenido =
            LimpiarRespuestaPhp(
                contenido);


        if (string.IsNullOrWhiteSpace(contenido))
        {
            return new RegistrarPagoResponse
            {
                Res = 0,
                Msg =
                    "El servidor no devolvió información al registrar el pago."
            };
        }


        var resultado =
            JsonSerializer.Deserialize<RegistrarPagoResponse>(
                contenido,
                JsonOptions);


        return resultado ??
               new RegistrarPagoResponse
               {
                   Res = 0,
                   Msg =
                       "El servidor devolvió una respuesta inválida."
               };
    }
    catch (JsonException ex)
    {
        return new RegistrarPagoResponse
        {
            Res = 0,
            Msg =
                $"La respuesta del pago no tiene un formato válido: {ex.Message}"
        };
    }
    catch (Exception ex)
    {
        return new RegistrarPagoResponse
        {
            Res = 0,
            Msg =
                $"No fue posible registrar el pago: {ex.Message}"
        };
    }
}


// =========================================================
// FINALIZAR VENTA
// =========================================================



public async Task<FinalizarVentaResponse>
    FinalizarVentaAsync(
        int idVenta)
{
    try
    {
        var response =
            await ApiClient.Http
                .PostAsJsonAsync(
                    $"api/pos/ventas/{idVenta}/finalizar",
                    new { }
                );

        var contenido =
            await response.Content
                .ReadAsStringAsync();


        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "RESPUESTA API FINALIZAR VENTA:");

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


        if (string.IsNullOrWhiteSpace(contenido))
        {
            return new FinalizarVentaResponse
            {
                Res = 0,
                Msg =
                    "El servidor no devolvió información al finalizar la venta."
            };
        }


        var resultado =
            JsonSerializer.Deserialize<FinalizarVentaResponse>(
                contenido,
                JsonOptions
            );


        return resultado ??
               new FinalizarVentaResponse
               {
                   Res = 0,
                   Msg =
                       "El servidor devolvió una respuesta inválida al finalizar la venta."
               };
    }
    catch (JsonException ex)
    {
        return new FinalizarVentaResponse
        {
            Res = 0,
            Msg =
                $"La respuesta al finalizar la venta no tiene un formato válido: {ex.Message}"
        };
    }
    catch (Exception ex)
    {
        return new FinalizarVentaResponse
        {
            Res = 0,
            Msg =
                $"No fue posible finalizar la venta: {ex.Message}"
        };
    }
}


    // =========================================================
    // LIMPIAR RESPUESTA PHP
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