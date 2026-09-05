using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using NovaCoreESDM.Models.Turno;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Turnos;


public class TurnosService
{
    // =========================================================
    // CONFIGURACIÓN JSON
    // =========================================================

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,

            /*
             * PostgreSQL/PDO puede devolver campos numeric
             * como texto:
             *
             * "200.00"
             *
             * Esto permite convertirlos correctamente
             * a decimal.
             */
            NumberHandling =
                JsonNumberHandling.AllowReadingFromString
        };


    // =========================================================
    // OBTENER TURNO ACTUAL
    // =========================================================

    public async Task<TurnoActualResponse> ObtenerTurnoActualAsync(
        int idCaja)
    {
        try
        {
            var endpoint =
                $"api/pos/turnos/actual?id_caja={idCaja}";


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
                "RESPUESTA API TURNO ACTUAL:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            // =================================================
            // LIMPIAR RESPUESTA PHP
            // =================================================

            contenido =
                LimpiarRespuestaPhp(
                    contenido);


            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new TurnoActualResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor no devolvió información del turno."
                };
            }


            // =================================================
            // DESERIALIZAR
            // =================================================

            try
            {
                var resultado =
                    JsonSerializer.Deserialize<TurnoActualResponse>(
                        contenido,
                        JsonOptions);


                if (resultado is null)
                {
                    return new TurnoActualResponse
                    {
                        Res = 0,

                        Msg =
                            "El servidor devolvió una respuesta inválida."
                    };
                }


                return resultado;
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "ERROR DESERIALIZANDO TURNO ACTUAL:");

                Console.WriteLine(ex);

                Console.WriteLine(
                    "====================================");


                return new TurnoActualResponse
                {
                    Res = 0,

                    Msg =
                        "La respuesta del servidor para el turno actual no es válida."
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR CONSULTANDO TURNO ACTUAL:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new TurnoActualResponse
            {
                Res = 0,

                Msg =
                    $"No fue posible consultar el turno actual: {ex.Message}"
            };
        }
    }
    
    
    // =========================================================
// OBTENER RESUMEN DEL TURNO
// =========================================================

public async Task<ResumenTurnoResponse> ObtenerResumenTurnoAsync(
    int idTurno)
{
    try
    {
        if (idTurno <= 0)
        {
            return new ResumenTurnoResponse
            {
                Res = 0,
                Msg = "El turno no es válido."
            };
        }


        var endpoint =
            $"api/pos/turnos/resumen?id_turno={idTurno}";


        var response =
            await ApiClient.Http.GetAsync(
                endpoint);


        var contenido =
            await response.Content
                .ReadAsStringAsync();


        // =====================================================
        // DEBUG
        // =====================================================

        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "RESPUESTA API RESUMEN TURNO:");

        Console.WriteLine(
            $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

        Console.WriteLine(
            contenido);

        Console.WriteLine(
            "====================================");


        // =====================================================
        // LIMPIAR RESPUESTA PHP
        // =====================================================

        contenido =
            LimpiarRespuestaPhp(
                contenido);


        if (string.IsNullOrWhiteSpace(contenido))
        {
            return new ResumenTurnoResponse
            {
                Res = 0,

                Msg =
                    "El servidor no devolvió información del resumen del turno."
            };
        }


        // =====================================================
        // DESERIALIZAR
        // =====================================================

        try
        {
            var resultado =
                JsonSerializer.Deserialize<ResumenTurnoResponse>(
                    contenido,
                    JsonOptions);


            if (resultado is null)
            {
                return new ResumenTurnoResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor devolvió una respuesta inválida para el resumen del turno."
                };
            }


            return resultado;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR DESERIALIZANDO RESUMEN TURNO:");

            Console.WriteLine(
                ex);

            Console.WriteLine(
                "====================================");


            return new ResumenTurnoResponse
            {
                Res = 0,

                Msg =
                    "La respuesta del servidor para el resumen del turno no es válida."
            };
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "ERROR CONSULTANDO RESUMEN TURNO:");

        Console.WriteLine(
            ex);

        Console.WriteLine(
            "====================================");


        return new ResumenTurnoResponse
        {
            Res = 0,

            Msg =
                $"No fue posible consultar el resumen del turno: {ex.Message}"
        };
    }
}


// =========================================================
// OBTENER ACTIVIDAD DEL TURNO
// =========================================================

public async Task<ActividadTurnoResponse> ObtenerActividadTurnoAsync(
    int idTurno,
    int limit = 10)
{
    try
    {
        if (idTurno <= 0)
        {
            return new ActividadTurnoResponse
            {
                Res = 0,
                Msg = "El turno no es válido."
            };
        }


        var endpoint =
            $"api/pos/turnos/actividad?id_turno={idTurno}&limit={limit}";


        var response =
            await ApiClient.Http.GetAsync(
                endpoint);


        var contenido =
            await response.Content
                .ReadAsStringAsync();


        // =====================================================
        // DEBUG
        // =====================================================

        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "RESPUESTA API ACTIVIDAD TURNO:");

        Console.WriteLine(
            $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

        Console.WriteLine(
            contenido);

        Console.WriteLine(
            "====================================");


        // =====================================================
        // LIMPIAR RESPUESTA PHP
        // =====================================================

        contenido =
            LimpiarRespuestaPhp(
                contenido);


        if (string.IsNullOrWhiteSpace(contenido))
        {
            return new ActividadTurnoResponse
            {
                Res = 0,

                Msg =
                    "El servidor no devolvió información de la actividad del turno."
            };
        }


        // =====================================================
        // DESERIALIZAR
        // =====================================================

        try
        {
            var resultado =
                JsonSerializer.Deserialize<ActividadTurnoResponse>(
                    contenido,
                    JsonOptions);


            if (resultado is null)
            {
                return new ActividadTurnoResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor devolvió una respuesta inválida para la actividad del turno."
                };
            }


            return resultado;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR DESERIALIZANDO ACTIVIDAD TURNO:");

            Console.WriteLine(
                ex);

            Console.WriteLine(
                "====================================");


            return new ActividadTurnoResponse
            {
                Res = 0,

                Msg =
                    "La respuesta del servidor para la actividad del turno no es válida."
            };
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "ERROR CONSULTANDO ACTIVIDAD TURNO:");

        Console.WriteLine(
            ex);

        Console.WriteLine(
            "====================================");


        return new ActividadTurnoResponse
        {
            Res = 0,

            Msg =
                $"No fue posible consultar la actividad del turno: {ex.Message}"
        };
    }
}






    // =========================================================
    // ABRIR TURNO
    // =========================================================

    public async Task<AbrirTurnoResponse> AbrirTurnoAsync(
        AbrirTurnoRequest request)
    {
        try
        {
            var response =
                await ApiClient.Http.PostAsJsonAsync(
                    "api/pos/turnos/abrir",
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
                "RESPUESTA API ABRIR TURNO:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            // =================================================
            // LIMPIAR RESPUESTA PHP
            // =================================================

            contenido =
                LimpiarRespuestaPhp(
                    contenido);


            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new AbrirTurnoResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor no devolvió información al abrir el turno."
                };
            }


            // =================================================
            // DESERIALIZAR
            // =================================================

            try
            {
                var resultado =
                    JsonSerializer.Deserialize<AbrirTurnoResponse>(
                        contenido,
                        JsonOptions);


                if (resultado is null)
                {
                    return new AbrirTurnoResponse
                    {
                        Res = 0,

                        Msg =
                            "El servidor devolvió una respuesta inválida."
                    };
                }


                return resultado;
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "ERROR DESERIALIZANDO APERTURA DE TURNO:");

                Console.WriteLine(ex);

                Console.WriteLine(
                    "====================================");


                return new AbrirTurnoResponse
                {
                    Res = 0,

                    Msg =
                        "La respuesta del servidor al abrir el turno no es válida."
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR ABRIENDO TURNO:");

            Console.WriteLine(ex);

            Console.WriteLine(
                "====================================");


            return new AbrirTurnoResponse
            {
                Res = 0,

                Msg =
                    $"No fue posible abrir el turno: {ex.Message}"
            };
        }
    }
    
    
    // =========================================================
    // CERRAR TURNO
    // =========================================================

    public async Task<CerrarTurnoResponse> CerrarTurnoAsync(
        CerrarTurnoRequest request)
    {
        try
        {
            // =====================================================
            // VALIDACIONES LOCALES
            // =====================================================

            if (request.IdTurno <= 0)
            {
                return new CerrarTurnoResponse
                {
                    Res = 0,

                    Msg =
                        "El turno no es válido."
                };
            }


            if (request.EfectivoDeclarado < 0m)
            {
                return new CerrarTurnoResponse
                {
                    Res = 0,

                    Msg =
                        "El efectivo declarado no puede ser negativo."
                };
            }


            // =====================================================
            // PETICIÓN
            // =====================================================

            var response =
                await ApiClient.Http.PostAsJsonAsync(
                    "api/pos/turnos/cerrar",
                    request);


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            // =====================================================
            // DEBUG
            // =====================================================

            Console.WriteLine(
                "====================================");


            Console.WriteLine(
                "RESPUESTA API CERRAR TURNO:");


            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");


            Console.WriteLine(
                contenido);


            Console.WriteLine(
                "====================================");


            // =====================================================
            // LIMPIAR RESPUESTA PHP
            // =====================================================

            contenido =
                LimpiarRespuestaPhp(
                    contenido);


            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new CerrarTurnoResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor no devolvió información al cerrar el turno."
                };
            }


            // =====================================================
            // DESERIALIZAR
            // =====================================================

            try
            {
                var resultado =
                    JsonSerializer.Deserialize<CerrarTurnoResponse>(
                        contenido,
                        JsonOptions);


                if (resultado is null)
                {
                    return new CerrarTurnoResponse
                    {
                        Res = 0,

                        Msg =
                            "El servidor devolvió una respuesta inválida al cerrar el turno."
                    };
                }


                return resultado;
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    "====================================");


                Console.WriteLine(
                    "ERROR DESERIALIZANDO CIERRE DE TURNO:");


                Console.WriteLine(
                    ex);


                Console.WriteLine(
                    "====================================");


                return new CerrarTurnoResponse
                {
                    Res = 0,

                    Msg =
                        "La respuesta del servidor al cerrar el turno no es válida."
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");


            Console.WriteLine(
                "ERROR CERRANDO TURNO:");


            Console.WriteLine(
                ex);


            Console.WriteLine(
                "====================================");


            return new CerrarTurnoResponse
            {
                Res = 0,

                Msg =
                    $"No fue posible cerrar el turno: {ex.Message}"
            };
        }
    }


    // =========================================================
    // LIMPIAR RESPUESTA PHP
    // =========================================================

    private static string LimpiarRespuestaPhp(
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return contenido;
        }


        /*
         * Protección temporal.
         *
         * Si PHP genera:
         *
         * Notice
         * Warning
         * Deprecated
         *
         * antes del JSON, buscamos el primer {
         * para recuperar solamente la respuesta JSON.
         */

        var inicioJson =
            contenido.IndexOf('{');


        return inicioJson >= 0
            ? contenido.Substring(inicioJson)
            : contenido;
    }
}