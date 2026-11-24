using NovaCoreESDM.Services.Offline;
using System;
using System.Text.Json;
using System.Threading.Tasks;

using NovaCoreESDM.Models.Clientes;
using NovaCoreESDM.Services.Api;


namespace NovaCoreESDM.Services.Clientes;


public class ClientesService
{
    private static readonly JsonSerializerOptions
        JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive =
                    true
            };


    // =========================================================
    // BUSCAR CLIENTES
    // =========================================================

    public async Task<BuscarClientesResponse>
        BuscarClientesAsync(
            string buscar)
    {
        buscar =
            buscar?.Trim()
            ?? string.Empty;


        if (buscar.Length < 2)
        {
            return new BuscarClientesResponse
            {
                Res = 1
            };
        }


        // Sin internet: clientes con crédito de la copia de la caja.
        if (!ConexionService.EstaEnLinea)
            return BuscarSinConexion(buscar);


        try
        {
            var termino =
                Uri.EscapeDataString(
                    buscar
                );


            var response =
                await ApiClient.Http
                    .GetAsync(
                        $"api/pos/clientes?buscar={termino}"
                    );


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA API CLIENTES:");

            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                contenido);

            Console.WriteLine(
                "====================================");


            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return new BuscarClientesResponse
                {
                    Res = 0,
                    Msg =
                        "El servidor no devolvió información de clientes."
                };
            }


            /*
             * Igual que en otros servicios:
             *
             * Si PHP imprime algún Notice/Warning antes
             * del JSON, intentamos quedarnos desde
             * el primer {.
             */

            var inicioJson =
                contenido.IndexOf('{');


            if (inicioJson >= 0)
            {
                contenido =
                    contenido.Substring(
                        inicioJson
                    );
            }


            var resultado =
                JsonSerializer
                    .Deserialize<BuscarClientesResponse>(
                        contenido,
                        JsonOptions
                    );


            return resultado
                   ?? new BuscarClientesResponse
                   {
                       Res = 0,
                       Msg =
                           "La respuesta de clientes es inválida."
                   };
        }
        catch (Exception ex)
        {
            if (ConexionService.EsErrorDeRed(ex))
            {
                ConexionService.ReportarFalloDeRed();
                return BuscarSinConexion(buscar);
            }

            return new BuscarClientesResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible buscar clientes: {ex.Message}"
            };
        }
    }


    private static BuscarClientesResponse BuscarSinConexion(string buscar) =>
        new()
        {
            Res = 1,
            Msg = "Sin conexión: solo se muestran clientes con crédito guardados en esta caja.",
            Data = CreditoLocal.Buscar(buscar)
        };
}
