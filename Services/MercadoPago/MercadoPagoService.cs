using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NovaCoreESDM.Desktop.Models.MercadoPago;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Desktop.Services.MercadoPago
{
    public class MercadoPagoService
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling =
                    JsonNumberHandling.AllowReadingFromString
            };


        public MercadoPagoService()
        {
            /*
             * MUY IMPORTANTE:
             *
             * Utilizamos el mismo HttpClient compartido que el resto
             * de NovaCore.
             *
             * NO creamos:
             *
             * new HttpClient()
             *
             * porque necesitamos conservar la sesión/cookies PHP
             * del usuario actualmente autenticado.
             */
            _http = ApiClient.Http;
        }


        // ============================================================
        // TERMINALES
        // ============================================================

        public async Task<MercadoPagoTerminalesResponse> ObtenerTerminalesAsync(
            long idVenta,
            CancellationToken cancellationToken = default)
        {
            if (idVenta <= 0)
                throw new ArgumentException(
                    "La venta indicada no es válida.",
                    nameof(idVenta));

            var url =
                $"api/pos/mercadopago/terminales?id_venta={idVenta}";

            Console.WriteLine("====================================");
            Console.WriteLine("MP SERVICE - SOLICITUD TERMINALES");
            Console.WriteLine($"URL RELATIVA: {url}");
            Console.WriteLine($"BASE ADDRESS: {_http.BaseAddress}");
            Console.WriteLine("====================================");

            using var response =
                await _http.GetAsync(
                    url,
                    cancellationToken);

            var contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            // =========================================================
            // DEBUG TEMPORAL - RESPUESTA REAL DEL SERVIDOR
            // =========================================================

            Console.WriteLine("====================================");
            Console.WriteLine("MP SERVICE - RESPUESTA RAW TERMINALES");
            Console.WriteLine(
                $"HTTP STATUS: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine(
                $"URL FINAL: {response.RequestMessage?.RequestUri}");
            Console.WriteLine("CONTENT-TYPE:");
            Console.WriteLine(
                response.Content.Headers.ContentType?.ToString()
                ?? "(sin content-type)");
            Console.WriteLine("CONTENIDO RAW:");
            Console.WriteLine(contenido);
            Console.WriteLine("====================================");

            contenido =
                LimpiarRespuestaPhp(contenido);

            var resultado =
                Deserializar<MercadoPagoTerminalesResponse>(
                    contenido,
                    "terminales de Mercado Pago");

            /*
             * Aunque el backend responda 4xx/5xx, intentamos primero
             * leer su JSON porque contiene el mensaje real de NovaCore.
             */
            if (!response.IsSuccessStatusCode &&
                resultado.Res != 1)
            {
                throw new MercadoPagoException(
                    resultado.Msg
                    ?? "No fue posible obtener las terminales Mercado Pago.",
                    (int)response.StatusCode);
            }

            if (resultado.Res != 1)
            {
                throw new MercadoPagoException(
                    resultado.Msg
                    ?? "No fue posible obtener las terminales Mercado Pago.",
                    (int)response.StatusCode);
            }

            return resultado;
        }


        // ============================================================
        // CREAR ORDER
        // ============================================================

        public async Task<MercadoPagoCrearOrdenResponse> CrearOrdenAsync(
            long idVenta,
            int idTpvBancaria,
            CancellationToken cancellationToken = default)
        {
            if (idVenta <= 0)
                throw new ArgumentException(
                    "La venta indicada no es válida.",
                    nameof(idVenta));

            if (idTpvBancaria <= 0)
                throw new ArgumentException(
                    "La terminal indicada no es válida.",
                    nameof(idTpvBancaria));


            var request =
                new MercadoPagoCrearOrdenRequest
                {
                    IdVenta = idVenta,
                    IdTpvBancaria = idTpvBancaria
                };


            var resultado =
                await PostAsync<
                    MercadoPagoCrearOrdenRequest,
                    MercadoPagoCrearOrdenResponse>(
                    "api/pos/mercadopago/crear-orden",
                    request,
                    cancellationToken);


            /*
             * Hay errores que son parte importante del flujo.
             *
             * No queremos perder esa información convirtiéndolos
             * inmediatamente en una excepción genérica.
             */
            if (resultado.Res != 1)
            {
                if (resultado.EstadoIncierto ||
                    resultado.TerminalOcupada ||
                    resultado.PagoCompleto)
                {
                    return resultado;
                }

                throw new MercadoPagoException(
                    resultado.Msg
                    ?? "No fue posible iniciar el cobro Mercado Pago.");
            }


            return resultado;
        }


        // ============================================================
        // CONSULTAR ESTADO
        // ============================================================

        public async Task<MercadoPagoEstadoOrdenResponse> ObtenerEstadoOrdenAsync(
            long idOperacion,
            CancellationToken cancellationToken = default)
        {
            if (idOperacion <= 0)
                throw new ArgumentException(
                    "La operación indicada no es válida.",
                    nameof(idOperacion));


            var url =
                $"api/pos/mercadopago/estado-orden?id_operacion={idOperacion}";


            using var response =
                await _http.GetAsync(
                    url,
                    cancellationToken);


            var contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);


            contenido =
                LimpiarRespuestaPhp(contenido);


            var resultado =
                Deserializar<MercadoPagoEstadoOrdenResponse>(
                    contenido,
                    "estado de Mercado Pago");


            /*
             * IMPORTANTE:
             *
             * Un error de comunicación/sincronización puede significar
             * que el dinero YA fue cobrado en la Point.
             *
             * Por eso EstadoIncierto y RequiereConciliacion regresan
             * al caller para que VentasViewModel NO inicie otro cobro.
             */
            if (resultado.Res != 1)
            {
                if (resultado.EstadoIncierto ||
                    resultado.RequiereConciliacion)
                {
                    return resultado;
                }


                throw new MercadoPagoException(
                    resultado.Msg
                    ?? "No fue posible consultar el estado del pago.",
                    (int)response.StatusCode);
            }


            return resultado;
        }


        // ============================================================
        // CANCELAR ORDER
        // ============================================================

        public async Task<MercadoPagoCancelarOrdenResponse> CancelarOrdenAsync(
            long idOperacion,
            CancellationToken cancellationToken = default)
        {
            if (idOperacion <= 0)
                throw new ArgumentException(
                    "La operación indicada no es válida.",
                    nameof(idOperacion));


            var request =
                new MercadoPagoCancelarOrdenRequest
                {
                    IdOperacion = idOperacion
                };


            var resultado =
                await PostAsync<
                    MercadoPagoCancelarOrdenRequest,
                    MercadoPagoCancelarOrdenResponse>(
                    "api/pos/mercadopago/cancelar-orden",
                    request,
                    cancellationToken);


            /*
             * Estos estados NO son errores genéricos.
             *
             * La UI necesita distinguirlos para decirle al cajero
             * qué debe hacer.
             */
            if (resultado.Res != 1)
            {
                if (resultado.CancelarDesdeTerminal ||
                    resultado.RequiereAccionTerminal ||
                    resultado.RequiereSincronizacion ||
                    resultado.RequiereVerificacion ||
                    resultado.RequiereRecuperacion ||
                    resultado.RequiereReembolso ||
                    resultado.EstadoIncierto)
                {
                    return resultado;
                }


                throw new MercadoPagoException(
                    resultado.Msg
                    ?? "No fue posible cancelar la operación Mercado Pago.");
            }


            return resultado;
        }


        // ============================================================
        // POLLING
        // ============================================================
        //
        // Este método será el corazón de la ventana:
        //
        //      Procesando pago...
        //             ◯
        //      Esperando pago en terminal
        //
        //
        // No bloquea el hilo de UI.
        //
        // Cada intervalo consulta nuestro PHP.
        //
        // NUNCA consulta directamente a Mercado Pago.
        // ============================================================

        public async Task<MercadoPagoResultadoCobro> EsperarResultadoAsync(
            long idOperacion,
            Action<string>? actualizarEstado = null,
            TimeSpan? intervalo = null,
            CancellationToken cancellationToken = default)
        {
            if (idOperacion <= 0)
                throw new ArgumentException(
                    "La operación indicada no es válida.",
                    nameof(idOperacion));


            /*
             * Dos segundos es suficientemente rápido para que
             * la interfaz se sienta inmediata sin bombardear
             * innecesariamente nuestro backend.
             */
            var espera =
                intervalo
                ?? TimeSpan.FromSeconds(2);


            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();


                MercadoPagoEstadoOrdenResponse respuesta;


                try
                {
                    respuesta =
                        await ObtenerEstadoOrdenAsync(
                            idOperacion,
                            cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    /*
                     * IMPORTANTE:
                     *
                     * Un error temporal de red NO significa que
                     * debamos iniciar otro cobro.
                     *
                     * Seguimos esperando y volvemos a consultar.
                     */
                    actualizarEstado?.Invoke(
                        "Reconectando con Mercado Pago...");


                    await Task.Delay(
                        espera,
                        cancellationToken);


                    continue;
                }


                /*
                 * ====================================================
                 * CONCILIACIÓN
                 * ====================================================
                 *
                 * Este caso es distinto a una simple desconexión.
                 *
                 * El backend pudo confirmar que MP acreditó dinero,
                 * pero tuvo un problema al reflejarlo localmente.
                 *
                 * NO podemos continuar indefinidamente ni generar
                 * otra Order.
                 * ====================================================
                 */

                if (respuesta.RequiereConciliacion)
                {
                    return new MercadoPagoResultadoCobro
                    {
                        Exitoso = false,
                        RequiereConciliacion = true,
                        IdOperacion =
                            respuesta.IdOperacionError
                            ?? idOperacion,
                        OrderId =
                            respuesta.OrderIdError,
                        Estado =
                            "CONCILIACION",
                        Mensaje =
                            respuesta.Msg
                            ?? "El pago requiere conciliación con NovaCore."
                    };
                }


                /*
                 * ====================================================
                 * ESTADO INCIERTO
                 * ====================================================
                 *
                 * Aquí no terminamos el cobro ni creamos otro.
                 *
                 * Esperamos y consultamos nuevamente.
                 * ====================================================
                 */

                if (respuesta.EstadoIncierto)
                {
                    actualizarEstado?.Invoke(
                        "Verificando el pago con Mercado Pago...");


                    await Task.Delay(
                        espera,
                        cancellationToken);


                    continue;
                }


                var data =
                    respuesta.Data;


                if (data == null)
                {
                    actualizarEstado?.Invoke(
                        respuesta.Msg
                        ?? "Esperando respuesta de Mercado Pago...");


                    await Task.Delay(
                        espera,
                        cancellationToken);


                    continue;
                }


                var estado =
                    (data.EstadoNovaCore ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();


                /*
                 * ====================================================
                 * APROBADO
                 * ====================================================
                 */

                if (data.PagoRegistrado ||
                    estado == "APROBADO")
                {
                    return new MercadoPagoResultadoCobro
                    {
                        Exitoso =
                            data.PagoRegistrado,

                        PagoRegistrado =
                            data.PagoRegistrado,

                        PagoCompleto =
                            data.PagoCompleto,

                        IdOperacion =
                            data.IdOperacion,

                        IdPagoPos =
                            data.IdPagoPos,

                        OrderId =
                            data.OrderId,

                        PaymentId =
                            data.PaymentId,

                        Estado =
                            "APROBADO",

                        Mensaje =
                            respuesta.Msg
                            ?? "Pago aprobado.",

                        Importe =
                            data.Importe
                    };
                }


                /*
                 * ====================================================
                 * RECHAZADO
                 * ====================================================
                 */

                if (estado == "RECHAZADO" ||
                    data.TerminoSinPago &&
                    string.Equals(
                        data.Status,
                        "failed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new MercadoPagoResultadoCobro
                    {
                        Exitoso = false,
                        Rechazado = true,

                        IdOperacion =
                            data.IdOperacion,

                        OrderId =
                            data.OrderId,

                        PaymentId =
                            data.PaymentId,

                        Estado =
                            "RECHAZADO",

                        Mensaje =
                            respuesta.Msg
                            ?? "El pago fue rechazado.",

                        Importe =
                            data.Importe
                    };
                }


                /*
                 * ====================================================
                 * CANCELADO
                 * ====================================================
                 */

                if (estado == "CANCELADO" ||
                    string.Equals(
                        data.Status,
                        "canceled",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new MercadoPagoResultadoCobro
                    {
                        Exitoso = false,
                        Cancelado = true,

                        IdOperacion =
                            data.IdOperacion,

                        OrderId =
                            data.OrderId,

                        Estado =
                            "CANCELADO",

                        Mensaje =
                            respuesta.Msg
                            ?? "La operación fue cancelada.",

                        Importe =
                            data.Importe
                    };
                }


                /*
                 * ====================================================
                 * EXPIRADO
                 * ====================================================
                 */

                if (estado == "EXPIRADO" ||
                    string.Equals(
                        data.Status,
                        "expired",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new MercadoPagoResultadoCobro
                    {
                        Exitoso = false,
                        Expirado = true,

                        IdOperacion =
                            data.IdOperacion,

                        OrderId =
                            data.OrderId,

                        Estado =
                            "EXPIRADO",

                        Mensaje =
                            respuesta.Msg
                            ?? "La operación expiró.",

                        Importe =
                            data.Importe
                    };
                }


                /*
                 * ====================================================
                 * ACCIÓN REQUERIDA
                 * ====================================================
                 */

                if (estado == "ACCION_REQUERIDA")
                {
                    actualizarEstado?.Invoke(
                        respuesta.Msg
                        ?? "Revise la terminal Mercado Pago.");
                }
                else
                {
                    /*
                     * created
                     * at_terminal
                     * processing
                     * etc.
                     */

                    actualizarEstado?.Invoke(
                        respuesta.Msg
                        ?? ObtenerMensajeEstado(estado));
                }


                await Task.Delay(
                    espera,
                    cancellationToken);
            }
        }


        // ============================================================
        // HELPERS HTTP
        // ============================================================

        private async Task<TResponse> PostAsync<TRequest, TResponse>(
            string url,
            TRequest request,
            CancellationToken cancellationToken)
        {
            var json =
                JsonSerializer.Serialize(
                    request,
                    JsonOptions);


            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");


            using var response =
                await _http.PostAsync(
                    url,
                    content,
                    cancellationToken);


            var contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);


            contenido =
                LimpiarRespuestaPhp(contenido);


            var resultado =
                Deserializar<TResponse>(
                    contenido,
                    url);


            /*
             * No hacemos EnsureSuccessStatusCode().
             *
             * Nuestros PHP utilizan correctamente 409, 502, 503,
             * etc. y el body JSON contiene información necesaria
             * para decidir qué debe hacer NovaCore.
             */
            return resultado;
        }


        private static T Deserializar<T>(
            string contenido,
            string contexto)
        {
            if (string.IsNullOrWhiteSpace(contenido))
            {
                throw new MercadoPagoException(
                    $"El servidor devolvió una respuesta vacía al consultar {contexto}.");
            }


            try
            {
                var resultado =
                    JsonSerializer.Deserialize<T>(
                        contenido,
                        JsonOptions);


                if (resultado == null)
                {
                    throw new MercadoPagoException(
                        $"No fue posible interpretar la respuesta de {contexto}.");
                }


                return resultado;
            }
            catch (JsonException ex)
            {
                throw new MercadoPagoException(
                    $"El servidor devolvió una respuesta inválida al consultar {contexto}.",
                    null,
                    ex);
            }
        }


        // ============================================================
        // LIMPIAR RESPUESTA PHP
        // ============================================================
        //
        // Conservamos el mismo comportamiento que ya utiliza NovaCore:
        //
        // Si PHP imprime accidentalmente:
        //
        // <br>Notice...</br>{"res":1,...}
        //
        // intentamos quedarnos desde el primer {
        //
        // ============================================================

        private static string LimpiarRespuestaPhp(
            string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return contenido;


            var inicioJson =
                contenido.IndexOf('{');


            if (inicioJson > 0)
            {
                contenido =
                    contenido.Substring(
                        inicioJson);
            }


            return contenido.Trim();
        }


        // ============================================================
        // MENSAJE VISUAL
        // ============================================================

        private static string ObtenerMensajeEstado(
            string estado)
        {
            return estado switch
            {
                "CREADA" =>
                    "Enviando cobro a la terminal Mercado Pago...",

                "ESPERANDO_TERMINAL" =>
                    "Esperando pago en la terminal Mercado Pago...",

                "PROCESANDO" =>
                    "Procesando pago...",

                "ACCION_REQUERIDA" =>
                    "Revise la terminal Mercado Pago...",

                "PENDIENTE" =>
                    "Esperando respuesta de Mercado Pago...",

                _ =>
                    "Esperando pago en Mercado Pago..."
            };
        }
    }


    // ================================================================
    // EXCEPCIÓN DEL MÓDULO
    // ================================================================

    public class MercadoPagoException : Exception
    {
        public int? HttpStatusCode { get; }


        public MercadoPagoException(
            string message,
            int? httpStatusCode = null,
            Exception? innerException = null)
            : base(
                message,
                innerException)
        {
            HttpStatusCode =
                httpStatusCode;
        }
    }
}