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
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,

            // PostgreSQL/PDO puede devolver numeric como texto:
            // "200.00"
            //
            // Esto permite convertirlo correctamente a decimal.
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
                await ApiClient.Http.GetAsync(endpoint);

            var contenido =
                await response.Content.ReadAsStringAsync();


            // =================================================
            // DEBUG TEMPORAL
            // =================================================

            Console.WriteLine("====================================");
            Console.WriteLine("RESPUESTA API TURNO ACTUAL:");
            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine(contenido);
            Console.WriteLine("====================================");


            contenido =
                LimpiarRespuestaPhp(contenido);


            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new TurnoActualResponse
                {
                    Res = 0,
                    Msg =
                        "El servidor no devolvió información del turno."
                };
            }


            TurnoActualResponse? resultado;


            try
            {
                resultado =
                    JsonSerializer.Deserialize<TurnoActualResponse>(
                        contenido,
                        JsonOptions);
            }
            catch (JsonException ex)
            {
                Console.WriteLine("====================================");
                Console.WriteLine(
                    "ERROR DESERIALIZANDO TURNO ACTUAL:");
                Console.WriteLine(ex);
                Console.WriteLine("====================================");

                return new TurnoActualResponse
                {
                    Res = 0,
                    Msg =
                        "La respuesta del servidor para el turno actual no es válida."
                };
            }


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
        catch (Exception ex)
        {
            Console.WriteLine("====================================");
            Console.WriteLine(
                "ERROR CONSULTANDO TURNO ACTUAL:");
            Console.WriteLine(ex);
            Console.WriteLine("====================================");

            return new TurnoActualResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible consultar el turno actual: {ex.Message}"
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
                await response.Content.ReadAsStringAsync();


            // =================================================
            // DEBUG TEMPORAL
            // =================================================

            Console.WriteLine("====================================");
            Console.WriteLine("RESPUESTA API ABRIR TURNO:");
            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine(contenido);
            Console.WriteLine("====================================");


            contenido =
                LimpiarRespuestaPhp(contenido);


            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new AbrirTurnoResponse
                {
                    Res = 0,
                    Msg =
                        "El servidor no devolvió información al abrir el turno."
                };
            }


            AbrirTurnoResponse? resultado;


            try
            {
                resultado =
                    JsonSerializer.Deserialize<AbrirTurnoResponse>(
                        contenido,
                        JsonOptions);
            }
            catch (JsonException ex)
            {
                Console.WriteLine("====================================");
                Console.WriteLine(
                    "ERROR DESERIALIZANDO APERTURA DE TURNO:");
                Console.WriteLine(ex);
                Console.WriteLine("====================================");

                return new AbrirTurnoResponse
                {
                    Res = 0,
                    Msg =
                        "La respuesta del servidor al abrir el turno no es válida."
                };
            }


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
        catch (Exception ex)
        {
            Console.WriteLine("====================================");
            Console.WriteLine(
                "ERROR ABRIENDO TURNO:");
            Console.WriteLine(ex);
            Console.WriteLine("====================================");

            return new AbrirTurnoResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible abrir el turno: {ex.Message}"
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
            return contenido;


        /*
         * Si por algún motivo PHP genera un Notice/Warning
         * antes del JSON, buscamos el primer {
         *
         * Cuando terminemos de limpiar todos los endpoints
         * podremos quitar esta protección.
         */
        var inicioJson =
            contenido.IndexOf('{');


        return inicioJson >= 0
            ? contenido.Substring(inicioJson)
            : contenido;
    }
}