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
// ACTUALIZAR CANTIDAD DE PRODUCTO
// =========================================================

public async Task<ActualizarCantidadVentaResponse>
    ActualizarCantidadProductoAsync(
        int idVenta,
        int idDetalle,
        decimal cantidadComercial)
{
    try
    {
        var request =
            new ActualizarCantidadVentaRequest
            {
                CantidadComercial =
                    cantidadComercial
            };

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
            "RESPUESTA API ACTUALIZAR CANTIDAD:");

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
            return new ActualizarCantidadVentaResponse
            {
                Res = 0,
                Msg =
                    "El servidor no devolvió información al actualizar la cantidad."
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
    catch (Exception ex)
    {
        return new ActualizarCantidadVentaResponse
        {
            Res = 0,
            Msg =
                $"No fue posible actualizar la cantidad: {ex.Message}"
        };
    }
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