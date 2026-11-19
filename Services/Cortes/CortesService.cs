using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using NovaCoreESDM.Models.Cortes;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Cortes;

public class CortesService
{
    private static readonly JsonSerializerOptions
        JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive =
                    true,

                NumberHandling =
                    JsonNumberHandling.AllowReadingFromString
            };


    // ============================================================
    // OBTENER HISTORIAL
    // ============================================================

    public async Task<CortesListResponse>
        ObtenerCortesAsync(
            int? idCaja = null,
            int? idTurno = null,
            string? fechaDesde = null,
            string? fechaHasta = null,
            int limit = 50)
    {
        try
        {
            var url =
                "api/pos/cortes";


            var parametros =
                new System.Collections.Generic.List<string>();


            if (idCaja.HasValue &&
                idCaja.Value > 0)
            {
                parametros.Add(
                    $"id_caja={idCaja.Value}");
            }


            if (idTurno.HasValue &&
                idTurno.Value > 0)
            {
                parametros.Add(
                    $"id_turno={idTurno.Value}");
            }


            if (!string.IsNullOrWhiteSpace(
                    fechaDesde))
            {
                parametros.Add(
                    $"fecha_desde={Uri.EscapeDataString(fechaDesde)}");
            }


            if (!string.IsNullOrWhiteSpace(
                    fechaHasta))
            {
                parametros.Add(
                    $"fecha_hasta={Uri.EscapeDataString(fechaHasta)}");
            }


            parametros.Add(
                $"limit={limit}");


            if (parametros.Count > 0)
            {
                url +=
                    "?" +
                    string.Join(
                        "&",
                        parametros);
            }


            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "CONSULTANDO HISTORIAL DE CORTES:");

            Console.WriteLine(
                url);

            Console.WriteLine(
                "========================================");


            var response =
                await ApiClient.Http
                    .GetAsync(
                        url);


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "RESPUESTA HISTORIAL DE CORTES:");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "========================================");


            contenido =
                LimpiarRespuestaPhp(
                    contenido);


            if (!response.IsSuccessStatusCode)
            {
                return new CortesListResponse
                {
                    Res = 0,

                    Msg =
                        $"HTTP {(int)response.StatusCode}: {contenido}"
                };
            }


            var resultado =
                JsonSerializer.Deserialize<CortesListResponse>(
                    contenido,
                    JsonOptions);


            if (resultado is null)
            {
                return new CortesListResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor devolvió una respuesta vacía."
                };
            }


            return resultado;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "ERROR CONSULTANDO HISTORIAL DE CORTES:");

            Console.WriteLine(
                ex);

            Console.WriteLine(
                "========================================");


            return new CortesListResponse
            {
                Res = 0,

                Msg =
                    $"No fue posible consultar los cortes: {ex.Message}"
            };
        }
    }


    // ============================================================
    // PRUEBA DE CORTE (SIMULACIÓN)
    // ============================================================
    //
    // GET /api/pos/cortes/simular
    //
    // Calcula el corte de una caja en cualquier periodo, con la
    // misma cuenta que el cierre de turno. No modifica nada.
    // ============================================================

    public async Task<SimulacionCorteResponse>
        SimularCorteAsync(
            int idCaja,
            DateTime fechaDesde,
            DateTime fechaHasta)
    {
        try
        {
            var url =
                "api/pos/cortes/simular" +
                $"?id_caja={idCaja}" +
                $"&fecha_desde={fechaDesde:yyyy-MM-dd}" +
                $"&fecha_hasta={fechaHasta:yyyy-MM-dd}";


            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "SIMULANDO CORTE:");

            Console.WriteLine(
                url);

            Console.WriteLine(
                "========================================");


            var response =
                await ApiClient.Http
                    .GetAsync(
                        url);


            var contenido =
                LimpiarRespuestaPhp(
                    await response.Content
                        .ReadAsStringAsync());


            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new SimulacionCorteResponse
                {
                    Res = 0,

                    Msg =
                        $"El servidor no devolvió información (HTTP {(int)response.StatusCode})."
                };
            }


            SimulacionCorteResponse? resultado;

            try
            {
                resultado =
                    JsonSerializer.Deserialize<SimulacionCorteResponse>(
                        contenido,
                        JsonOptions);
            }
            catch (JsonException)
            {
                Console.WriteLine(
                    "RESPUESTA SIMULACIÓN NO VÁLIDA:");

                Console.WriteLine(
                    contenido);

                return new SimulacionCorteResponse
                {
                    Res = 0,

                    Msg =
                        response.StatusCode == System.Net.HttpStatusCode.NotFound
                            ? "El servidor todavía no tiene la Prueba de corte. Hay que publicar el backend."
                            : $"La respuesta del servidor no es válida (HTTP {(int)response.StatusCode})."
                };
            }


            if (resultado is not null &&
                resultado.Res != 1 &&
                (resultado.Msg ?? string.Empty).Contains(
                    "Endpoint no existe",
                    StringComparison.OrdinalIgnoreCase))
            {
                resultado.Msg =
                    "El servidor todavía no tiene la Prueba de corte. Hay que publicar el backend (api/pos/cortes/simular).";
            }


            return resultado
                ?? new SimulacionCorteResponse
                {
                    Res = 0,

                    Msg =
                        "El servidor devolvió una respuesta vacía."
                };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "ERROR SIMULANDO CORTE:");

            Console.WriteLine(
                ex);

            return new SimulacionCorteResponse
            {
                Res = 0,

                Msg =
                    $"No fue posible simular el corte: {ex.Message}"
            };
        }
    }


    // ============================================================
    // LIMPIAR RESPUESTA PHP
    // ============================================================

    private static string LimpiarRespuestaPhp(
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(
                contenido))
        {
            return contenido;
        }


        var inicioJsonObjeto =
            contenido.IndexOf(
                '{');


        var inicioJsonArray =
            contenido.IndexOf(
                '[');


        var inicioJson =
            -1;


        if (inicioJsonObjeto >= 0 &&
            inicioJsonArray >= 0)
        {
            inicioJson =
                Math.Min(
                    inicioJsonObjeto,
                    inicioJsonArray);
        }
        else if (inicioJsonObjeto >= 0)
        {
            inicioJson =
                inicioJsonObjeto;
        }
        else if (inicioJsonArray >= 0)
        {
            inicioJson =
                inicioJsonArray;
        }


        if (inicioJson > 0)
        {
            contenido =
                contenido.Substring(
                    inicioJson);
        }


        return contenido.Trim();
    }
}