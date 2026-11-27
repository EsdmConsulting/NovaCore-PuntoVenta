using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Ventas;
using NovaCoreESDM.Services.Api;
using NovaCoreESDM.Services.Offline;

namespace NovaCoreESDM.Services.Ventas;

/// <summary>
/// Terminales bancarias de la caja, del catálogo del ERP (Comercial › TPV
/// bancarias). Al cobrar con tarjeta el cajero elige con cuál cobró y el
/// pago se guarda con esa terminal. Se guarda una copia por caja para que,
/// sin internet, se pueda elegir igual.
/// </summary>
public static class TerminalesBancariasService
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);

    private static int _idCajaMemoria;
    private static DateTime _cargadaEn;
    private static List<TerminalBancariaCaja>? _memoria;


    private static string Ruta(int idCaja) => AlmacenLocal.Ruta($"terminales-bancarias-{idCaja}.json");


    /// <summary>
    /// Lista para el cobro. Nunca falla: sin internet o con un servidor
    /// que todavía no tiene el cambio, regresa la copia guardada (o vacía,
    /// y entonces el cobro queda como antes, sin elegir terminal).
    /// </summary>
    public static async Task<List<TerminalBancariaCaja>> ObtenerAsync(int idCaja)
    {
        if (idCaja <= 0)
            return new List<TerminalBancariaCaja>();

        if (_memoria is not null && _idCajaMemoria == idCaja && DateTime.Now - _cargadaEn < Vigencia)
            return _memoria;

        if (!ConexionService.EstaEnLinea)
            return LeerCopia(idCaja);

        try
        {
            var response = await ApiClient.Http.GetAsync($"api/pos/cajas/{idCaja}/terminales-bancarias");
            var contenido = await response.Content.ReadAsStringAsync();
            var inicio = contenido.IndexOf('{');

            if (inicio < 0)
                return LeerCopia(idCaja);

            var resultado = JsonSerializer.Deserialize<TerminalesBancariasResponse>(contenido[inicio..], Json);

            if (resultado?.Res != 1 || resultado.Data is null)
                return LeerCopia(idCaja);

            var lista = resultado.Data.Terminales
                .Where(t => t.IdTpvBancaria > 0)
                .OrderByDescending(t => t.EsPredeterminada)
                .ThenBy(t => t.Banco)
                .ThenBy(t => t.Nombre)
                .ToList();

            try
            {
                AlmacenLocal.Guardar(Ruta(idCaja), lista);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se guardó la copia de terminales: {ex.Message}");
            }

            return Recordar(idCaja, lista);
        }
        catch (Exception ex) when (ConexionService.EsErrorDeRed(ex))
        {
            ConexionService.ReportarFalloDeRed();
            return LeerCopia(idCaja);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR TERMINALES BANCARIAS: {ex.Message}");
            return LeerCopia(idCaja);
        }
    }


    private static List<TerminalBancariaCaja> LeerCopia(int idCaja)
    {
        try
        {
            return AlmacenLocal.Leer<List<TerminalBancariaCaja>>(Ruta(idCaja)) ?? new List<TerminalBancariaCaja>();
        }
        catch
        {
            return new List<TerminalBancariaCaja>();
        }
    }


    private static List<TerminalBancariaCaja> Recordar(int idCaja, List<TerminalBancariaCaja> lista)
    {
        _idCajaMemoria = idCaja;
        _cargadaEn = DateTime.Now;
        _memoria = lista;
        return lista;
    }
}
