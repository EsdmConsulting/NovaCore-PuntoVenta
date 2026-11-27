using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Configuration;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.TerminalPago;
using NovaCoreESDM.Services.Api;
using NovaCoreESDM.Services.Configuration;

namespace NovaCoreESDM.Services.TerminalPago;

/// <summary>
/// Antes de cancelar una venta pagada con el lector del banco, regresa el
/// cargo de la tarjeta del cliente:
///
///  - CANCELACIÓN si sigue el mismo día bancario (antes de la 1:00 a.m. CDMX)
///    y es por el total: no pide la tarjeta.
///  - DEVOLUCIÓN en otro caso: el lector pide la tarjeta del cliente.
///
/// El puente decide cuál. Después se deja constancia en el ERP
/// (POST /api/pos/pagos-tarjeta/{id}/reverso).
///
/// Si un cargo no se pudo regresar, la venta NO se debe cancelar (el
/// cliente seguiría con el cargo): se regresa el motivo.
/// </summary>
public sealed class ReversoTarjetaService
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromMinutes(3);

    private readonly TerminalPagoClient _client = new();


    public sealed class Resultado
    {
        public bool Ok { get; init; }
        public string Mensaje { get; init; } = string.Empty;
        public int Regresados { get; init; }
    }


    /// <summary>Cobros con el lector de la venta (vacío si no hay o si el servidor no tiene el cambio).</summary>
    public static async Task<List<PagoTarjetaVenta>> ObtenerAsync(long idVenta)
    {
        try
        {
            var r = await ApiClient.Http.GetAsync($"api/pos/ventas/{idVenta}/pagos-tarjeta");

            if (!r.IsSuccessStatusCode)
                return new();

            var texto = await r.Content.ReadAsStringAsync();
            var inicio = texto.IndexOf('{');

            if (inicio < 0)
                return new();

            var datos = JsonSerializer.Deserialize<PagosTarjetaVentaResponse>(texto[inicio..], Json);

            return datos?.Res == 1 && datos.Data is not null ? datos.Data : new();
        }
        catch
        {
            return new();
        }
    }


    /// <summary>
    /// Regresa todos los cargos vigentes de la venta. <paramref name="avance"/>
    /// recibe los textos para el cajero ("Pase la tarjeta del cliente…").
    /// </summary>
    public async Task<Resultado> RegresarCargosAsync(long idVenta, Action<string>? avance)
    {
        var pagos = (await ObtenerAsync(idVenta)).Where(p => p.Vigente).ToList();

        if (pagos.Count == 0)
            return new Resultado { Ok = true };

        var lectores = new TerminalPagoConfigurationService()
            .ObtenerActivas()
            .Where(t => t.EsInalambrica)
            .ToList();

        var regresados = 0;

        foreach (var pago in pagos)
        {
            if (string.IsNullOrWhiteSpace(pago.ReferenciaOperacion))
                return Falla($"El cobro con {pago.Descripcion} no tiene la referencia del lector. Regrésalo desde el portal del banco.", regresados);

            avance?.Invoke($"Buscando el cobro con {pago.Descripcion} en el lector…");

            var (lector, cobro) = await BuscarAsync(lectores, pago.ReferenciaOperacion);

            if (lector is null || cobro is null)
            {
                return Falla(
                    $"No se encontró el cobro con {pago.Descripcion} en el lector de esta caja. " +
                    "Cancela la venta en la caja donde se cobró.", regresados);
            }

            // Ya se había regresado en el banco (p. ej. se cayó la red al avisar al ERP).
            if (cobro.Reverso is { Aprobado: true })
            {
                await AvisarAlErpAsync(pago, cobro.Reverso);
                regresados++;
                continue;
            }

            var envio = await _client.ReversarAsync(lector, cobro.Referencia, pago.Importe, PosSession.Usuario);

            if (!envio.Ok || envio.Dato is null)
                return Falla($"No se pudo regresar el cargo de {pago.Descripcion}: {envio.Mensaje}", regresados);

            var final = await EsperarAsync(lector, envio.Dato, pago, avance);

            if (final is null)
            {
                return Falla(
                    $"El lector no confirmó el regreso de {pago.Descripcion}. No canceles todavía: revisa el lector y vuelve a intentar.",
                    regresados);
            }

            if (!final.Aprobado)
            {
                var motivo = string.IsNullOrWhiteSpace(final.Motivo)
                    ? CodigosRespuestaBanco.Texto(final.CodigoRespuesta)
                    : final.Motivo;

                return Falla($"El banco no aceptó regresar {pago.Descripcion}: {motivo}", regresados);
            }

            await AvisarAlErpAsync(pago, final);
            regresados++;
        }

        return new Resultado
        {
            Ok = true,
            Regresados = regresados,
            Mensaje = regresados == 1
                ? "Se regresó el cargo de la tarjeta."
                : $"Se regresaron {regresados} cargos con tarjeta."
        };
    }


    private async Task<(TerminalPagoConfiguration?, CobroTerminal?)> BuscarAsync(
        List<TerminalPagoConfiguration> lectores, string referencia)
    {
        foreach (var lector in lectores)
        {
            var r = await _client.ConsultarAsync(lector, referencia);

            if (r.Ok && r.Dato is not null)
                return (lector, r.Dato);
        }

        return (null, null);
    }


    private async Task<ReversoTerminal?> EsperarAsync(
        TerminalPagoConfiguration lector, CobroTerminal cobro, PagoTarjetaVenta pago, Action<string>? avance)
    {
        var limite = DateTime.Now + EsperaMaxima;

        while (DateTime.Now < limite)
        {
            var rev = cobro.Reverso;

            if (rev is not null && !rev.Abierto)
                return rev;

            if (rev is not null)
            {
                var tipo = rev.Tipo == "CANCELACION" ? "Cancelando" : "Devolviendo";

                avance?.Invoke(rev.Etapa == "ESPERANDO_TARJETA"
                    ? $"Devolución de {pago.Descripcion}: pide al cliente que inserte o acerque su tarjeta en el lector."
                    : $"{tipo} el cargo de {pago.Descripcion} con el banco…");
            }

            await Task.Delay(1000);

            var consulta = await _client.ConsultarAsync(lector, cobro.Referencia);

            if (consulta.Ok && consulta.Dato is not null)
                cobro = consulta.Dato;
        }

        return null;
    }


    /// <summary>El banco ya regresó el cargo: queda registrado en el ERP (si falla, se reintenta al cancelar otra vez).</summary>
    private static async Task AvisarAlErpAsync(PagoTarjetaVenta pago, ReversoTerminal rev)
    {
        try
        {
            await ApiClient.Http.PostAsJsonAsync(
                $"api/pos/pagos-tarjeta/{pago.IdPago}/reverso",
                new
                {
                    tipo = rev.Tipo,
                    importe = rev.ImporteCentavos / 100m,
                    autorizacion = rev.Autorizacion,
                    referencia_financiera = rev.ReferenciaFinanciera
                });
        }
        catch (HttpRequestException)
        {
            // El cargo ya se regresó en el banco; el ERP se actualiza al reintentar.
        }
    }


    private static Resultado Falla(string mensaje, int regresados) =>
        new()
        {
            Ok = false,
            Regresados = regresados,
            Mensaje = regresados > 0
                ? $"{mensaje} (Ya se regresaron {regresados} cargo(s); al reintentar no se repiten.)"
                : mensaje
        };
}
