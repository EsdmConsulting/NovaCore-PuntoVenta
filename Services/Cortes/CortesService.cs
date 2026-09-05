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