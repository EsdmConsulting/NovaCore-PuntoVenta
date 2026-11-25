using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Devoluciones;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Devoluciones;

/// <summary>
/// CHG-0006: consulta el ticket y registra la devolución en el servidor
/// (módulo CEDIS → Devoluciones + salida de efectivo del turno abierto).
/// </summary>
public class DevolucionesService
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };


    public async Task<TicketDevolucionResponse> BuscarTicketAsync(string folio)
    {
        try
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var response = await ApiClient.Http.GetAsync(
                $"api/pos/devoluciones/ticket?folio={Uri.EscapeDataString(folio.Trim())}" +
                $"&id_caja={NovaCoreESDM.Models.Session.PosSession.IdCaja}&id_turno={NovaCoreESDM.Models.Session.PosSession.IdTurno}",
                limite.Token);

            var contenido = await response.Content.ReadAsStringAsync(limite.Token);
            Console.WriteLine($"RESPUESTA API DEVOLUCION TICKET ({(int)response.StatusCode}): {contenido}");

            return Leer<TicketDevolucionResponse>(contenido)
                   ?? new TicketDevolucionResponse
                   {
                       Res = 0,
                       Msg = response.StatusCode == HttpStatusCode.NotFound
                           ? "No se encontró el ticket (o el servidor todavía no tiene Devoluciones)."
                           : $"El servidor respondió {(int)response.StatusCode} al buscar el ticket."
                   };
        }
        catch (Exception ex) when (ex is OperationCanceledException or System.Net.Http.HttpRequestException)
        {
            return new TicketDevolucionResponse { Res = 0, Msg = "No hay conexión con el servidor. Las devoluciones necesitan internet." };
        }
    }


    public async Task<RegistrarDevolucionResponse> RegistrarAsync(RegistrarDevolucionRequest request)
    {
        try
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            using var response = await ApiClient.Http.PostAsJsonAsync(
                "api/pos/devoluciones", request, limite.Token);

            var contenido = await response.Content.ReadAsStringAsync(limite.Token);
            Console.WriteLine($"RESPUESTA API REGISTRAR DEVOLUCION ({(int)response.StatusCode}): {contenido}");

            return Leer<RegistrarDevolucionResponse>(contenido)
                   ?? new RegistrarDevolucionResponse
                   {
                       Res = 0,
                       Msg = $"El servidor respondió {(int)response.StatusCode} al registrar la devolución."
                   };
        }
        catch (Exception ex) when (ex is OperationCanceledException or System.Net.Http.HttpRequestException)
        {
            // No se sabe si llegó: el reintento usa el mismo uuid_operacion.
            return new RegistrarDevolucionResponse
            {
                Res = 0,
                Codigo = "SIN_CONFIRMAR",
                Msg = "Se perdió la conexión y no se pudo confirmar la devolución. NO entregues el dinero todavía: vuelve a intentar (no se duplica)."
            };
        }
    }


    private static T? Leer<T>(string contenido) where T : class
    {
        var inicio = contenido.IndexOf('{');
        if (inicio < 0)
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(contenido[inicio..], Opciones);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
