using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Desktop.Models.MercadoPago;
using NovaCoreESDM.Desktop.Services.MercadoPago;

namespace NovaCoreESDM.Desktop.ViewModels.MercadoPago
{
    public partial class MercadoPagoPagoViewModel : ObservableObject, IDisposable
    {
        // ============================================================
        // SERVICIO
        // ============================================================

        private readonly MercadoPagoService _mercadoPagoService;


        // ============================================================
        // DATOS DEL COBRO
        // ============================================================

        private readonly long _idVenta;
        private readonly int _idTpvBancaria;


        // ============================================================
        // CANCELACIÓN LOCAL DEL POLLING
        // ============================================================

        private CancellationTokenSource? _pollingCancellationTokenSource;


        // ============================================================
        // CONTROL INTERNO
        // ============================================================

        private bool _procesoIniciado;
        private bool _disposed;


        // ============================================================
        // PROPIEDADES VISUALES
        // ============================================================

        [ObservableProperty]
        private decimal _importe;


        [ObservableProperty]
        private string _nombreTerminal = string.Empty;


        [ObservableProperty]
        private string _mensajeEstado =
            "Preparando cobro...";


        [ObservableProperty]
        private string _titulo =
            "Procesando pago";


        [ObservableProperty]
        private bool _estaProcesando;


        [ObservableProperty]
        private bool _puedeCancelar;


        /*
         * Indica que la operación ya llegó a un estado en el que
         * podemos permitir que el cajero cierre la ventana.
         *
         * IMPORTANTE:
         *
         * PuedeCerrar = true NO significa necesariamente que el pago
         * haya sido exitoso.
         *
         * También puede significar:
         *
         * - rechazado;
         * - expirado;
         * - terminal ocupada;
         * - error;
         * - conciliación requerida.
         *
         * El Resultado sigue siendo quien determina qué ocurrió.
         */
        [ObservableProperty]
        private bool _puedeCerrar;


        [ObservableProperty]
        private bool _mostrarError;


        [ObservableProperty]
        private string _mensajeError =
            string.Empty;


        [ObservableProperty]
        private long? _idOperacion;


        [ObservableProperty]
        private string? _orderId;


        // ============================================================
        // RESULTADO FINAL
        // ============================================================

        public MercadoPagoResultadoCobro? Resultado { get; private set; }


        // ============================================================
        // EVENTOS PARA LA VIEW
        // ============================================================

        public event Action? SolicitarCerrar;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public MercadoPagoPagoViewModel(
            long idVenta,
            int idTpvBancaria,
            decimal importe,
            string? nombreTerminal = null)
        {
            if (idVenta <= 0)
            {
                throw new ArgumentException(
                    "La venta indicada no es válida.",
                    nameof(idVenta));
            }


            if (idTpvBancaria <= 0)
            {
                throw new ArgumentException(
                    "La terminal indicada no es válida.",
                    nameof(idTpvBancaria));
            }


            if (importe <= 0)
            {
                throw new ArgumentException(
                    "El importe debe ser mayor a cero.",
                    nameof(importe));
            }


            _idVenta =
                idVenta;


            _idTpvBancaria =
                idTpvBancaria;


            Importe =
                importe;


            NombreTerminal =
                string.IsNullOrWhiteSpace(nombreTerminal)
                    ? "Mercado Pago Point"
                    : nombreTerminal.Trim();


            _mercadoPagoService =
                new MercadoPagoService();


            EstaProcesando =
                false;


            PuedeCancelar =
                false;


            PuedeCerrar =
                false;
        }


        // ============================================================
        // INICIAR COBRO
        // ============================================================

        public async Task IniciarAsync()
        {
            if (_procesoIniciado)
                return;


            _procesoIniciado =
                true;


            MostrarError =
                false;


            MensajeError =
                string.Empty;


            EstaProcesando =
                true;


            PuedeCancelar =
                false;


            PuedeCerrar =
                false;


            Titulo =
                "Procesando pago";


            MensajeEstado =
                "Enviando cobro a la terminal Mercado Pago...";


            try
            {
                // ====================================================
                // 1. CREAR ORDER
                // ====================================================

                var crear =
                    await _mercadoPagoService.CrearOrdenAsync(
                        _idVenta,
                        _idTpvBancaria);


                // ====================================================
                // 2. VENTA YA PAGADA
                // ====================================================

                if (crear.PagoCompleto)
                {
                    Resultado =
                        new MercadoPagoResultadoCobro
                        {
                            Exitoso = true,
                            PagoRegistrado = true,
                            PagoCompleto = true,
                            Estado = "YA_PAGADA",
                            Mensaje =
                                crear.Msg
                                ?? "La venta ya se encuentra pagada.",
                            Importe =
                                Importe
                        };


                    EstaProcesando =
                        false;


                    PuedeCancelar =
                        false;


                    PuedeCerrar =
                        true;


                    MensajeEstado =
                        "La venta ya se encuentra pagada.";


                    SolicitarCerrar?.Invoke();

                    return;
                }


                // ====================================================
                // 3. TERMINAL OCUPADA
                // ====================================================

                if (crear.TerminalOcupada)
                {
                    Resultado =
                        new MercadoPagoResultadoCobro
                        {
                            Exitoso = false,
                            Estado = "TERMINAL_OCUPADA",
                            Mensaje =
                                crear.Msg
                                ?? "La terminal Mercado Pago está ocupada.",
                            Importe =
                                Importe
                        };


                    EstaProcesando =
                        false;


                    PuedeCancelar =
                        false;


                    PuedeCerrar =
                        true;


                    MostrarError =
                        true;


                    MensajeError =
                        Resultado.Mensaje;


                    return;
                }


                // ====================================================
                // 4. ESTADO INCIERTO AL CREAR
                // ====================================================

                if (crear.EstadoIncierto)
                {
                    if (
                        crear.IdOperacionError.HasValue
                        &&
                        crear.IdOperacionError.Value > 0
                    )
                    {
                        IdOperacion =
                            crear.IdOperacionError.Value;


                        PuedeCerrar =
                            false;


                        MensajeEstado =
                            "Recuperando comunicación con Mercado Pago...";


                        /*
                         * IMPORTANTE:
                         *
                         * No generamos una operación nueva.
                         *
                         * CrearOrdenAsync() volverá al backend y éste
                         * deberá reutilizar la operación/idempotency key
                         * que ya existe para esta venta.
                         */
                        await RecuperarCreacionAsync();

                        return;
                    }


                    Resultado =
                        new MercadoPagoResultadoCobro
                        {
                            Exitoso = false,
                            RequiereConciliacion = true,
                            Estado = "ESTADO_INCIERTO",
                            Mensaje =
                                crear.Msg
                                ?? "No fue posible confirmar el estado del cobro.",
                            Importe =
                                Importe
                        };


                    EstaProcesando =
                        false;


                    PuedeCancelar =
                        false;


                    PuedeCerrar =
                        true;


                    MostrarError =
                        true;


                    MensajeError =
                        Resultado.Mensaje;


                    return;
                }


                // ====================================================
                // 5. ORDER CREADA / RECUPERADA
                // ====================================================

                if (crear.Data == null)
                {
                    throw new MercadoPagoException(
                        "El servidor no devolvió los datos de la operación Mercado Pago.");
                }


                IdOperacion =
                    crear.Data.IdOperacion;


                OrderId =
                    crear.Data.OrderId;


                if (IdOperacion <= 0)
                {
                    throw new MercadoPagoException(
                        "El servidor no devolvió un identificador válido para la operación.");
                }


                /*
                 * Desde este momento tenemos una operación real.
                 *
                 * No permitimos cerrar directamente la ventana.
                 */
                PuedeCerrar =
                    false;


                PuedeCancelar =
                    true;


                MensajeEstado =
                    !string.IsNullOrWhiteSpace(crear.Msg)
                        ? crear.Msg
                        : "Esperando pago en la terminal Mercado Pago...";


                // ====================================================
                // 6. POLLING
                // ====================================================

                await EsperarPagoAsync();
            }
            catch (OperationCanceledException)
            {
                /*
                 * Detener el CancellationToken únicamente detiene
                 * nuestro polling local.
                 *
                 * NO significa que la Order de Mercado Pago haya sido
                 * cancelada.
                 */
            }
            catch (Exception ex)
            {
                EstaProcesando =
                    false;


                PuedeCancelar =
                    false;


                PuedeCerrar =
                    true;


                MostrarError =
                    true;


                MensajeError =
                    ex.Message;


                Resultado =
                    new MercadoPagoResultadoCobro
                    {
                        Exitoso = false,

                        /*
                         * Si alcanzamos a obtener IdOperacion,
                         * NO afirmamos que el pago falló.
                         *
                         * Debe verificarse antes de volver a cobrar.
                         */
                        RequiereConciliacion =
                            IdOperacion.HasValue,

                        IdOperacion =
                            IdOperacion,

                        OrderId =
                            OrderId,

                        Estado =
                            IdOperacion.HasValue
                                ? "REQUIERE_VERIFICACION"
                                : "ERROR",

                        Mensaje =
                            ex.Message,

                        Importe =
                            Importe
                    };
            }
        }


        // ============================================================
        // RECUPERAR CREACIÓN
        // ============================================================

        private async Task RecuperarCreacionAsync()
        {
            const int maxIntentos =
                5;


            for (
                var intento = 1;
                intento <= maxIntentos;
                intento++
            )
            {
                MensajeEstado =
                    $"Recuperando operación Mercado Pago... ({intento}/{maxIntentos})";


                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(2));


                    var recuperar =
                        await _mercadoPagoService.CrearOrdenAsync(
                            _idVenta,
                            _idTpvBancaria);


                    // ====================================================
                    // VENTA YA PAGADA
                    // ====================================================

                    if (recuperar.PagoCompleto)
                    {
                        Resultado =
                            new MercadoPagoResultadoCobro
                            {
                                Exitoso = true,
                                PagoRegistrado = true,
                                PagoCompleto = true,
                                Estado = "YA_PAGADA",
                                Mensaje =
                                    recuperar.Msg
                                    ?? "La venta ya se encuentra pagada.",
                                Importe =
                                    Importe
                            };


                        EstaProcesando =
                            false;


                        PuedeCancelar =
                            false;


                        PuedeCerrar =
                            true;


                        SolicitarCerrar?.Invoke();

                        return;
                    }


                    // ====================================================
                    // TERMINAL OCUPADA DURANTE RECUPERACIÓN
                    // ====================================================

                    if (recuperar.TerminalOcupada)
                    {
                        Resultado =
                            new MercadoPagoResultadoCobro
                            {
                                Exitoso = false,
                                RequiereConciliacion = true,
                                IdOperacion =
                                    IdOperacion,
                                OrderId =
                                    OrderId,
                                Estado =
                                    "TERMINAL_OCUPADA",
                                Mensaje =
                                    recuperar.Msg
                                    ?? "La terminal tiene una operación pendiente.",
                                Importe =
                                    Importe
                            };


                        EstaProcesando =
                            false;


                        PuedeCancelar =
                            false;


                        PuedeCerrar =
                            true;


                        MostrarError =
                            true;


                        MensajeError =
                            Resultado.Mensaje;


                        return;
                    }


                    // ====================================================
                    // ORDER RECUPERADA
                    // ====================================================

                    if (
                        recuperar.Res == 1
                        &&
                        recuperar.Data != null
                        &&
                        recuperar.Data.IdOperacion > 0
                        &&
                        !string.IsNullOrWhiteSpace(
                            recuperar.Data.OrderId)
                    )
                    {
                        IdOperacion =
                            recuperar.Data.IdOperacion;


                        OrderId =
                            recuperar.Data.OrderId;


                        PuedeCerrar =
                            false;


                        PuedeCancelar =
                            true;


                        MensajeEstado =
                            recuperar.Msg
                            ?? "Esperando pago en la terminal Mercado Pago...";


                        await EsperarPagoAsync();

                        return;
                    }


                    /*
                     * Sigue incierto.
                     *
                     * Esperamos y repetimos sobre la misma operación
                     * administrada por el backend.
                     */
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    /*
                     * Una desconexión temporal durante recuperación
                     * NO justifica crear otro cobro.
                     */
                }
            }


            // ========================================================
            // NO FUE POSIBLE RECUPERAR AUTOMÁTICAMENTE
            // ========================================================

            Resultado =
                new MercadoPagoResultadoCobro
                {
                    Exitoso = false,
                    RequiereConciliacion = true,
                    IdOperacion =
                        IdOperacion,
                    OrderId =
                        OrderId,
                    Estado =
                        "REQUIERE_CONCILIACION",
                    Mensaje =
                        "No fue posible recuperar automáticamente la operación Mercado Pago. No realice otro cobro hasta verificar esta operación.",
                    Importe =
                        Importe
                };


            EstaProcesando =
                false;


            PuedeCancelar =
                false;


            /*
             * Permitimos salir de la ventana.
             *
             * Esto NO autoriza otro cobro.
             * VentasView recibirá RequiereConciliacion=true.
             */
            PuedeCerrar =
                true;


            MostrarError =
                true;


            MensajeError =
                Resultado.Mensaje;
        }


        // ============================================================
        // ESPERAR PAGO
        // ============================================================

        private async Task EsperarPagoAsync()
        {
            if (
                !IdOperacion.HasValue
                ||
                IdOperacion.Value <= 0
            )
            {
                throw new MercadoPagoException(
                    "No existe una operación Mercado Pago válida para consultar.");
            }


            _pollingCancellationTokenSource?.Dispose();


            _pollingCancellationTokenSource =
                new CancellationTokenSource();


            PuedeCerrar =
                false;


            var resultado =
                await _mercadoPagoService.EsperarResultadoAsync(
                    IdOperacion.Value,

                    mensaje =>
                    {
                        MensajeEstado =
                            mensaje;
                    },

                    intervalo:
                        TimeSpan.FromSeconds(2),

                    cancellationToken:
                        _pollingCancellationTokenSource.Token);


            Resultado =
                resultado;


            EstaProcesando =
                false;


            PuedeCancelar =
                false;


            // ========================================================
            // APROBADO
            // ========================================================

            if (
                resultado.Exitoso
                &&
                resultado.PagoRegistrado
            )
            {
                PuedeCerrar =
                    true;


                Titulo =
                    "Pago aprobado";


                MensajeEstado =
                    resultado.Mensaje
                    ?? "Pago aprobado correctamente.";


                SolicitarCerrar?.Invoke();

                return;
            }


            // ========================================================
            // RECHAZADO
            // ========================================================

            if (resultado.Rechazado)
            {
                PuedeCerrar =
                    true;


                Titulo =
                    "Pago rechazado";


                MostrarError =
                    true;


                MensajeError =
                    resultado.Mensaje
                    ?? "La tarjeta fue rechazada.";


                return;
            }


            // ========================================================
            // CANCELADO
            // ========================================================

            if (resultado.Cancelado)
            {
                PuedeCerrar =
                    true;


                Titulo =
                    "Pago cancelado";


                MensajeEstado =
                    resultado.Mensaje
                    ?? "La operación fue cancelada.";


                SolicitarCerrar?.Invoke();

                return;
            }


            // ========================================================
            // EXPIRADO
            // ========================================================

            if (resultado.Expirado)
            {
                PuedeCerrar =
                    true;


                Titulo =
                    "Operación expirada";


                MostrarError =
                    true;


                MensajeError =
                    resultado.Mensaje
                    ?? "La operación Mercado Pago expiró.";


                return;
            }


            // ========================================================
            // CONCILIACIÓN
            // ========================================================

            if (resultado.RequiereConciliacion)
            {
                PuedeCerrar =
                    true;


                Titulo =
                    "Verificando pago";


                MostrarError =
                    true;


                MensajeError =
                    resultado.Mensaje
                    ?? "La operación requiere verificación antes de realizar otro cobro.";


                return;
            }


            // ========================================================
            // OTRO RESULTADO
            // ========================================================

            PuedeCerrar =
                true;


            MostrarError =
                true;


            MensajeError =
                resultado.Mensaje
                ?? "La operación Mercado Pago terminó sin un resultado reconocido.";
        }
        
        // ============================================================
        // CERRAR VENTANA
        // ============================================================

        [RelayCommand]
        private void Cerrar()
        {
            if (!PuedeCerrar)
                return;


            SolicitarCerrar?.Invoke();
        }


        // ============================================================
        // CANCELAR
        // ============================================================

        [RelayCommand]
        private async Task CancelarAsync()
        {
            if (
                !PuedeCancelar
                ||
                !IdOperacion.HasValue
                ||
                IdOperacion.Value <= 0
            )
            {
                return;
            }


            /*
             * Mientras verificamos la cancelación:
             *
             * - no permitimos otra cancelación;
             * - no permitimos cerrar la ventana.
             */
            PuedeCancelar =
                false;


            PuedeCerrar =
                false;


            MensajeEstado =
                "Verificando si el cobro puede cancelarse...";


            try
            {
                var respuesta =
                    await _mercadoPagoService.CancelarOrdenAsync(
                        IdOperacion.Value);


                // ====================================================
                // CANCELADA
                // ====================================================

                if (
                    respuesta.Res == 1
                    &&
                    respuesta.Data?.Cancelada == true
                )
                {
                    await DetenerPollingAsync();


                    Resultado =
                        new MercadoPagoResultadoCobro
                        {
                            Exitoso = false,
                            Cancelado = true,
                            IdOperacion =
                                IdOperacion,
                            OrderId =
                                respuesta.Data.OrderId,
                            Estado =
                                "CANCELADO",
                            Mensaje =
                                respuesta.Msg
                                ?? "La operación fue cancelada.",
                            Importe =
                                Importe
                        };


                    EstaProcesando =
                        false;


                    PuedeCancelar =
                        false;


                    PuedeCerrar =
                        true;


                    Titulo =
                        "Pago cancelado";


                    MensajeEstado =
                        Resultado.Mensaje;


                    SolicitarCerrar?.Invoke();

                    return;
                }


                // ====================================================
                // YA ESTÁ EN LA POINT
                // ====================================================

                if (respuesta.CancelarDesdeTerminal)
                {
                    /*
                     * NO detenemos polling.
                     *
                     * La Point todavía tiene la operación.
                     */
                    EstaProcesando =
                        true;


                    PuedeCancelar =
                        false;


                    PuedeCerrar =
                        false;


                    Titulo =
                        "Cancelar en terminal";


                    MensajeEstado =
                        "La operación ya está en la Point. Cancélela directamente desde la terminal Mercado Pago.";


                    return;
                }


                // ====================================================
                // YA SE PROCESÓ
                // ====================================================

                if (respuesta.RequiereSincronizacion)
                {
                    EstaProcesando =
                        true;


                    PuedeCancelar =
                        false;


                    PuedeCerrar =
                        false;


                    Titulo =
                        "Confirmando pago";


                    MensajeEstado =
                        "El pago ya fue procesado. Confirmando resultado con Mercado Pago...";


                    return;
                }


                // ====================================================
                // REQUIERE ACCIÓN EN POINT
                // ====================================================

                if (respuesta.RequiereAccionTerminal)
                {
                    EstaProcesando =
                        true;


                    PuedeCancelar =
                        false;


                    PuedeCerrar =
                        false;


                    Titulo =
                        "Revise la terminal";


                    MensajeEstado =
                        respuesta.Msg
                        ?? "Revise la terminal Mercado Pago.";


                    return;
                }


                // ====================================================
                // ESTADO INCIERTO / VERIFICACIÓN
                // ====================================================

                if (
                    respuesta.EstadoIncierto
                    ||
                    respuesta.RequiereVerificacion
                    ||
                    respuesta.RequiereRecuperacion
                )
                {
                    EstaProcesando =
                        true;


                    PuedeCancelar =
                        false;


                    PuedeCerrar =
                        false;


                    Titulo =
                        "Verificando operación";


                    MensajeEstado =
                        respuesta.Msg
                        ?? "Verificando el estado real de la operación...";


                    return;
                }


                // ====================================================
                // REEMBOLSO / PAGO YA ACREDITADO
                // ====================================================

                if (respuesta.RequiereReembolso)
                {
                    await DetenerPollingAsync();


                    Resultado =
                        new MercadoPagoResultadoCobro
                        {
                            Exitoso = false,
                            RequiereConciliacion = true,
                            IdOperacion =
                                IdOperacion,
                            OrderId =
                                respuesta.Data?.OrderId,
                            PaymentId =
                                respuesta.Data?.PaymentId,
                            Estado =
                                "PAGO_YA_ACREDITADO",
                            Mensaje =
                                respuesta.Msg
                                ?? "El pago ya fue acreditado.",
                            Importe =
                                Importe
                        };


                    EstaProcesando =
                        false;


                    PuedeCancelar =
                        false;


                    /*
                     * Dejamos salir al cajero, pero VentasView
                     * recibirá RequiereConciliacion=true y NO
                     * realizará otro cobro automáticamente.
                     */
                    PuedeCerrar =
                        true;


                    MostrarError =
                        true;


                    MensajeError =
                        Resultado.Mensaje;


                    return;
                }


                // ====================================================
                // RESPUESTA NO RECONOCIDA
                // ====================================================

                /*
                 * No conocemos todavía el estado real.
                 *
                 * Conservamos el polling y bloqueamos el cierre.
                 */
                EstaProcesando =
                    true;


                PuedeCancelar =
                    false;


                PuedeCerrar =
                    false;


                MensajeEstado =
                    respuesta.Msg
                    ?? "Verificando operación Mercado Pago...";
            }
            catch (Exception ex)
            {
                /*
                 * Si falla la petición de cancelación:
                 *
                 * NO asumimos que la Order fue cancelada.
                 * NO detenemos polling.
                 * NO permitimos cerrar todavía.
                 *
                 * La operación podría seguir viva o incluso
                 * haberse pagado.
                 */

                EstaProcesando =
                    true;


                PuedeCancelar =
                    false;


                PuedeCerrar =
                    false;


                Titulo =
                    "Verificando operación";


                MensajeEstado =
                    "No fue posible confirmar la cancelación. Verificando el estado del pago...";


                MostrarError =
                    true;


                MensajeError =
                    ex.Message;
            }
        }


        // ============================================================
        // DETENER POLLING LOCAL
        // ============================================================

        private async Task DetenerPollingAsync()
        {
            if (_pollingCancellationTokenSource == null)
                return;


            try
            {
                await _pollingCancellationTokenSource
                    .CancelAsync();
            }
            catch
            {
                /*
                 * No queremos que un error al detener el token
                 * cambie el resultado real de Mercado Pago.
                 */
            }
        }


        // ============================================================
        // DISPOSE
        // ============================================================

        public void Dispose()
        {
            if (_disposed)
                return;


            _disposed =
                true;


            try
            {
                _pollingCancellationTokenSource?.Cancel();
            }
            catch
            {
                // Ignorar.
            }


            _pollingCancellationTokenSource?.Dispose();


            _pollingCancellationTokenSource =
                null;
        }
    }
}