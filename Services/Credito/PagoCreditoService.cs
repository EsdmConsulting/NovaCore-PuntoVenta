using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using NovaCoreESDM.Models.Credito;
using NovaCoreESDM.Services.Api;
using NovaCoreESDM.Services.Offline;

namespace NovaCoreESDM.Services.Credito;

public class PagoCreditoService
{
    // =========================================================
    // OPCIONES JSON
    // =========================================================

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,

            NumberHandling =
                JsonNumberHandling
                    .AllowReadingFromString
        };


    // =========================================================
    // OBTENER RESUMEN DEL CLIENTE
    // =========================================================

    public async Task<ResumenCreditoResponse>
        ObtenerResumenClienteAsync(
            int idCliente)
    {
        try
        {
            if (idCliente <= 0)
            {
                return new ResumenCreditoResponse
                {
                    Res = 0,
                    Msg = "El cliente no es válido."
                };
            }


            // Sin internet: con la copia de crédito de la caja.
            if (!ConexionService.EstaEnLinea)
                return ResumenSinConexion(idCliente);


            var response =
                await ApiClient.Http
                    .GetAsync(
                        $"api/comercial/credito/resumen-cliente?id_cliente={idCliente}"
                    );


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA RESUMEN DE CRÉDITO:");

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
                return new ResumenCreditoResponse
                {
                    Res = 0,
                    Msg = "El servidor no devolvió información del crédito."
                };
            }


            var resultado =
                JsonSerializer
                    .Deserialize<ResumenCreditoResponse>(
                        contenido,
                        JsonOptions
                    );


            return resultado
                   ?? new ResumenCreditoResponse
                   {
                       Res = 0,
                       Msg = "El servidor devolvió una respuesta inválida."
                   };
        }
        catch (Exception ex) when (ConexionService.EsErrorDeRed(ex))
        {
            ConexionService.ReportarFalloDeRed();
            return ResumenSinConexion(idCliente);
        }
        catch (Exception ex)
        {
            return new ResumenCreditoResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible consultar el crédito: {ex.Message}"
            };
        }
    }


    // =========================================================
    // REGISTRAR ABONO
    // =========================================================

    public async Task<RegistrarAbonoCreditoResponse>
        RegistrarAbonoAsync(
            RegistrarAbonoCreditoRequest request,
            string nombreCliente = "")
    {
        if (request is null)
        {
            return new RegistrarAbonoCreditoResponse
            {
                Res = 0,
                Msg = "No existe información para registrar el abono."
            };
        }

        // Se fija antes de enviar: si se corta la red a la mitad, el mismo
        // uuid se reenvía después y el servidor no lo aplica dos veces.
        request.UuidOperacion ??= Guid.NewGuid().ToString();

        if (!ConexionService.EstaEnLinea)
            return GuardarSinConexion(request, nombreCliente);

        try
        {


            var response =
                await ApiClient.Http
                    .PostAsJsonAsync(
                        "api/comercial/credito/registrar-abono",
                        request,
                        JsonOptions
                    );


            var contenido =
                await response.Content
                    .ReadAsStringAsync();


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "RESPUESTA REGISTRAR ABONO DE CRÉDITO:");

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
                return new RegistrarAbonoCreditoResponse
                {
                    Res = 0,
                    Msg = "El servidor no devolvió información del abono."
                };
            }


            var resultado =
                JsonSerializer
                    .Deserialize<RegistrarAbonoCreditoResponse>(
                        contenido,
                        JsonOptions
                    );


            return resultado
                   ?? new RegistrarAbonoCreditoResponse
                   {
                       Res = 0,
                       Msg = "El servidor devolvió una respuesta inválida."
                   };
        }
        catch (Exception ex) when (ConexionService.EsErrorDeRed(ex))
        {
            // No se sabe si el servidor lo alcanzó a aplicar: se encola con el
            // mismo uuid (si ya estaba aplicado, el servidor solo lo confirma).
            ConexionService.ReportarFalloDeRed();
            return GuardarSinConexion(request, nombreCliente);
        }
        catch (Exception ex)
        {
            return new RegistrarAbonoCreditoResponse
            {
                Res = 0,
                Msg =
                    $"No fue posible registrar el abono: {ex.Message}"
            };
        }
    }


    // =========================================================
    // LIMPIAR RESPUESTA PHP
    // =========================================================
    //
    // Algunos endpoints pueden traer Notices o Warning
    // antes del JSON.
    //
    // Buscamos el primer {
    // y trabajamos desde ahí.
    //
    // =========================================================

    private static string LimpiarRespuestaPhp(
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(
                contenido))
        {
            return string.Empty;
        }


        var inicioJson =
            contenido.IndexOf(
                '{'
            );


        if (inicioJson >= 0)
        {
            contenido =
                contenido.Substring(
                    inicioJson
                );
        }


        return contenido.Trim();
    }


    // =========================================================
    // SIN CONEXIÓN
    // =========================================================

    private static ResumenCreditoResponse ResumenSinConexion(int idCliente)
    {
        var resumen = CreditoLocal.Resumen(idCliente);

        return resumen is null
            ? new ResumenCreditoResponse
            {
                Res = 0,
                Msg = "Sin conexión: este cliente no está en la copia de crédito de la caja."
            }
            : new ResumenCreditoResponse
            {
                Res = 1,
                Msg = $"Sin conexión: saldos de la copia de la caja ({CreditoLocal.FechaCopia:dd/MM/yyyy HH:mm}).",
                Data = resumen
            };
    }


    private static RegistrarAbonoCreditoResponse GuardarSinConexion(
        RegistrarAbonoCreditoRequest request, string nombreCliente)
    {
        try
        {
            // Saldos antes del abono (copia local), para el comprobante en pantalla.
            var antes = CreditoLocal.Resumen(request.IdCliente)?.DocumentosPendientes
                ?? new System.Collections.Generic.List<DocumentoCreditoPendiente>();

            var abono = AbonosPendientesService.Encolar(request, nombreCliente);

            var aplicados = request.Aplicaciones
                .Select(a =>
                {
                    var doc = antes.FirstOrDefault(d => d.Id == a.IdDocumentoCredito);
                    var saldo = doc?.Saldo ?? 0m;
                    return new DocumentoCreditoAplicado
                    {
                        IdDocumentoCredito = a.IdDocumentoCredito,
                        TipoDocumento = doc?.TipoDocumento ?? string.Empty,
                        Folio = doc?.Folio ?? a.IdDocumentoCredito.ToString(),
                        MontoAplicado = a.Monto,
                        SaldoAnterior = saldo,
                        SaldoNuevo = Math.Max(0m, saldo - a.Monto),
                        EstadoAnterior = doc?.Estado ?? 0,
                        EstadoNuevo = doc?.Estado ?? 0
                    };
                })
                .ToList();

            return new RegistrarAbonoCreditoResponse
            {
                Res = 1,
                GuardadoSinConexion = true,
                Msg = "Abono guardado en la caja. Se aplicará en cuanto regrese el internet.",
                Data = new RegistrarAbonoCreditoData
                {
                    ModoAplicacion = request.ModoAplicacion,
                    Monto = abono.Total,
                    MontoAplicado = abono.Total,
                    EstadoAbono = "GUARDADO SIN CONEXIÓN",
                    DocumentosAfectados = request.Aplicaciones.Count,
                    DocumentosAplicados = aplicados
                }
            };
        }
        catch (Exception ex)
        {
            return new RegistrarAbonoCreditoResponse
            {
                Res = 0,
                Msg = $"No se pudo guardar el abono en la caja: {ex.Message}"
            };
        }
    }
}
