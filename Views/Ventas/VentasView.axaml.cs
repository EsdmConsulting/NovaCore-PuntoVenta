using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using NovaCoreESDM.Models;
using NovaCoreESDM.Models.Productos;
using NovaCoreESDM.Models.Tickets;

using NovaCoreESDM.Services.Configuration;

using NovaCoreESDM.ViewModels.Ventas;
using Avalonia.VisualTree;

using NovaCoreESDM.Desktop.Services.MercadoPago;
using NovaCoreESDM.Desktop.Views.MercadoPago;

using NovaCoreESDM.Desktop.Services.MercadoPago;
using NovaCoreESDM.Desktop.Views.MercadoPago;

namespace NovaCoreESDM.Views.Ventas;



public partial class VentasView : UserControl
{
    private readonly TerminalConfigurationService
        _terminalConfigurationService;

    private readonly MercadoPagoService
        _mercadoPagoService;

    private VentasViewModel?
        _viewModel;


    private bool
        _productosCargados;


    public VentasView()
    {
        AvaloniaXamlLoader.Load(this);


        _terminalConfigurationService =
            new TerminalConfigurationService();
        
        _mercadoPagoService =
            new MercadoPagoService();


        AttachedToVisualTree +=
            OnAttachedToVisualTree;


        DetachedFromVisualTree +=
            OnDetachedFromVisualTree;
    }

    

    // =========================================================
    // ADJUNTAR VISTA
    // =========================================================

    private async void OnAttachedToVisualTree(
        object? sender,
        Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not VentasViewModel viewModel)
            return;


        if (_viewModel != viewModel)
        {
            // =================================================
            // DESVINCULAR VIEWMODEL ANTERIOR
            // =================================================

            if (_viewModel is not null)
            {
                _viewModel.SolicitarSeleccionPresentacion -=
                    OnSolicitarSeleccionPresentacion;


                _viewModel.SolicitarEditarCantidad -=
                    OnSolicitarEditarCantidad;


                _viewModel.SolicitarCobro -=
                    OnSolicitarCobro;


                _viewModel.SolicitarEntregaTicket -=
                    OnSolicitarEntregaTicket;


                _viewModel.SolicitarTelefonoWhatsApp -=
                    OnSolicitarTelefonoWhatsApp;


                _viewModel.SolicitarConfirmacionVentaFinalizada -=
                    OnSolicitarConfirmacionVentaFinalizada;
                
                _viewModel.SolicitarCambioPrecio -=
                    OnSolicitarCambioPrecio;
                
            }


            // =================================================
            // GUARDAR VIEWMODEL
            // =================================================

            _viewModel =
                viewModel;


            // =================================================
            // VINCULAR EVENTOS
            // =================================================

            _viewModel.SolicitarSeleccionPresentacion +=
                OnSolicitarSeleccionPresentacion;


            _viewModel.SolicitarEditarCantidad +=
                OnSolicitarEditarCantidad;


            _viewModel.SolicitarCobro +=
                OnSolicitarCobro;


            _viewModel.SolicitarEntregaTicket +=
                OnSolicitarEntregaTicket;


            _viewModel.SolicitarTelefonoWhatsApp +=
                OnSolicitarTelefonoWhatsApp;


            _viewModel.SolicitarConfirmacionVentaFinalizada +=
                OnSolicitarConfirmacionVentaFinalizada;
            
            _viewModel.SolicitarCambioPrecio +=
                OnSolicitarCambioPrecio;
        }


        if (_productosCargados)
        {
            EnfocarBuscadorProductos();

            return;
        }


        _productosCargados =
            true;


        await viewModel
            .CargarProductosAsync();


        await viewModel
            .CargarVentaActualAsync();
        
        EnfocarBuscadorProductos();

        // =====================================================
        // DEJAR ESCÁNER LISTO
        // =====================================================

        EnfocarBuscadorProductos();
    }


// =========================================================
// ESCÁNER / ENTER
// =========================================================

    private async void BuscadorProductosTextBox_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        if (_viewModel is null)
            return;

        var codigo =
            BuscadorProductosTextBox
                ?.Text
                ?.Trim();

        if (string.IsNullOrWhiteSpace(codigo))
            return;

        try
        {
            await _viewModel
                .ProcesarCodigoEscaneadoAsync(
                    codigo
                );

            BuscadorProductosTextBox.Text =
                string.Empty;
        }
        catch (Exception ex)
        {
            _viewModel.MensajeError =
                $"No fue posible procesar el código: {ex.Message}";
        }

        // NO reenfocar por ahora
    }


    // =========================================================
    // SELECCIONAR PRESENTACIÓN
    // =========================================================

    private async void OnSolicitarSeleccionPresentacion(
        ProductoCatalogo producto)
    {
        if (_viewModel is null)
            return;


        try
        {
            var configuracion =
                await _terminalConfigurationService
                    .ObtenerConfiguracionAsync();


            if (configuracion is null)
            {
                _viewModel.MensajeError =
                    "No existe una caja configurada para este equipo.";

                return;
            }


            var owner =
                TopLevel.GetTopLevel(this)
                as Window;


            if (owner is null)
            {
                _viewModel.MensajeError =
                    "No fue posible abrir el selector de presentación.";

                return;
            }


            var selector =
                new SeleccionarPresentacionWindow(
                    producto,
                    configuracion.UnidadCodigo
                );


            var resultado =
                await selector
                    .ShowDialog<bool>(
                        owner
                    );


            if (
                !resultado ||
                selector.PresentacionSeleccionada is null
            )
            {
                return;
            }


            await _viewModel
                .AgregarPresentacionAlCarritoAsync(
                    producto,
                    selector.PresentacionSeleccionada
                );
        }
        finally
        {
            /*
             * Tanto si agrega como si cancela,
             * regresamos al escáner.
             */
            EnfocarBuscadorProductos();
        }
    }
    
    // =========================================================
    // CAMBIAR PRECIO
    // =========================================================

    private async void OnSolicitarCambioPrecio(
        DetalleVenta detalle)
    {
        if (_viewModel is null)
            return;

        try
        {
            var window =
                new SeleccionarPrecioWindow(
                    detalle
                );

            var parentWindow =
                TopLevel.GetTopLevel(this)
                    as Window;

            if (parentWindow is null)
            {
                _viewModel.MensajeError =
                    "No fue posible abrir el selector de precio.";

                return;
            }

            var resultado =
                await window
                    .ShowDialog<SeleccionPrecioResultado?>(
                        parentWindow
                    );

            if (resultado is null)
                return;


            if (resultado.EsAutomatico)
            {
                await _viewModel
                    .EstablecerPrecioAutomaticoAsync(
                        detalle
                    );

                return;
            }


            if (
                string.IsNullOrWhiteSpace(
                    resultado.TipoPrecio
                )
            )
            {
                _viewModel.MensajeError =
                    "No se seleccionó un tipo de precio válido.";

                return;
            }


            await _viewModel
                .EstablecerPrecioManualAsync(
                    detalle,
                    resultado.TipoPrecio
                );
        }
        catch (Exception ex)
        {
            _viewModel.MensajeError =
                $"No fue posible cambiar el precio: {ex.Message}";
        }
    }


    // =========================================================
    // EDITAR CANTIDAD
    // =========================================================

    private async void OnSolicitarEditarCantidad(
        DetalleVenta detalle)
    {
        if (_viewModel is null)
            return;


        try
        {
            var owner =
                TopLevel.GetTopLevel(this)
                as Window;


            if (owner is null)
            {
                _viewModel.MensajeError =
                    "No fue posible abrir el editor de cantidad.";

                return;
            }


            var ventana =
                new EditarCantidadWindow(
                    detalle.Cantidad
                );


            var resultado =
                await ventana
                    .ShowDialog<bool>(
                        owner
                    );


            if (!resultado)
                return;


            await _viewModel
                .ActualizarCantidadManualAsync(
                    detalle,
                    ventana.CantidadSeleccionada
                );
        }
        finally
        {
            EnfocarBuscadorProductos();
        }
    }
    

    // =========================================================
    // COBRAR
    // =========================================================

    private async void OnSolicitarCobro()
    {
        if (_viewModel is null)
            return;


        try
        {
            var owner =
                TopLevel.GetTopLevel(this)
                as Window;


            if (owner is null)
            {
                _viewModel.MensajeError =
                    "No fue posible abrir la ventana de cobro.";

                return;
            }


            // =========================================================
            // FLUJO DE COBRO
            // =========================================================

            while (true)
            {
                // =====================================================
                // SELECTOR DE MÉTODO DE PAGO
                // =====================================================

                var ventana =
                    new CobroWindow(
                        _viewModel.Total,
                        _viewModel
                    );


                var resultado =
                    await ventana
                        .ShowDialog<bool>(
                            owner
                        );


                // =====================================================
                // CERRÓ / CANCELÓ TODO EL COBRO
                // =====================================================

                if (!resultado)
                    return;


                // =====================================================
                // EFECTIVO
                // =====================================================

                if (
                    ventana.MetodoSeleccionado ==
                    "EFECTIVO"
                )
                {
                    var pagoEfectivo =
                        new PagoEfectivoWindow(
                            _viewModel.Total
                        );


                    var pagoConfirmado =
                        await pagoEfectivo
                            .ShowDialog<bool>(
                                owner
                            );


                    // =================================================
                    // CANCELÓ EFECTIVO
                    // =================================================
                    //
                    // Regresamos al selector de método de pago.
                    // =================================================

                    if (!pagoConfirmado)
                        continue;


                    // =================================================
                    // REGISTRAR PAGO
                    // =================================================

                    var pagoRegistrado =
                        await _viewModel
                            .RegistrarPagoEfectivoAsync(
                                _viewModel.Total,
                                pagoEfectivo.CantidadRecibida,
                                pagoEfectivo.Cambio
                            );


                    if (!pagoRegistrado)
                        return;


                    // =================================================
                    // PAGO TERMINADO
                    // =================================================

                    return;
                }


                // =====================================================
                // CRÉDITO
                // =====================================================

                if (
                    ventana.MetodoSeleccionado ==
                    "CREDITO"
                )
                {
                    // =================================================
                    // FINALIZAR VENTA A CRÉDITO
                    // =================================================
                    //
                    // Este método:
                    //
                    // 1. Actualiza tipo_venta = CREDITO.
                    // 2. Conserva al cliente seleccionado.
                    // 3. Llama FinalizarVentaAsync().
                    // 4. El backend vuelve a validar el crédito.
                    // 5. Genera REMISIÓN PPD + CARGO.
                    // 6. Descuenta inventario.
                    // 7. Genera ticket.
                    // =================================================

                    var ventaCreditoFinalizada =
                        await _viewModel
                            .FinalizarVentaCreditoAsync();


                    // =================================================
                    // ERROR / RECHAZO
                    // =================================================

                    if (!ventaCreditoFinalizada)
                    {
                        /*
                         * No hacemos retry automático.
                         *
                         * MensajeError contendrá el motivo
                         * entregado por backend.
                         */

                        return;
                    }


                    // =================================================
                    // VENTA A CRÉDITO TERMINADA
                    // =================================================

                    return;
                }
                
                
                // =====================================================
                // TARJETA / MERCADO PAGO
                // =====================================================

                if (
                    ventana.MetodoSeleccionado ==
                    "TARJETA"
                )
                {
                    // =================================================
                    // VALIDAR VENTA
                    // =================================================

                    if (_viewModel.IdVentaActual <= 0)
                    {
                        _viewModel.MensajeError =
                            "No existe una venta activa para cobrar.";

                        return;
                    }


                    try
                    {
                        // =================================================
                        // 1. CONSULTAR TERMINALES MERCADO PAGO
                        // =================================================
                       
                        Console.WriteLine("====================================");
                        Console.WriteLine("MP: CONSULTANDO TERMINALES...");
                        Console.WriteLine($"ID VENTA: {_viewModel.IdVentaActual}");
                        Console.WriteLine("====================================");
                        

                        var terminales =
                            await _mercadoPagoService
                                .ObtenerTerminalesAsync(
                                    _viewModel.IdVentaActual
                                );
                        
                        Console.WriteLine("====================================");
                        Console.WriteLine("MP: RESPUESTA DE TERMINALES RECIBIDA");
                        Console.WriteLine($"RES: {terminales.Res}");
                        Console.WriteLine($"MSG: {terminales.Msg}");
                        Console.WriteLine(
                            $"TERMINALES: {terminales.Data?.Terminales.Count ?? 0}"
                        );
                        Console.WriteLine("====================================");


                        if (
                            terminales.Data is null
                            ||
                            terminales.Data.Terminales.Count == 0
                        )
                        {
                            _viewModel.MensajeError =
                                "No existen terminales Mercado Pago disponibles.";

                            continue;
                        }


                        // =================================================
                        // 2. DETERMINAR TERMINAL
                        // =================================================

                        var terminalSeleccionada =
                            terminales.Data.Terminales.Count == 1
                                ? terminales.Data.Terminales[0]
                                : null;


                        // =================================================
                        // 3. SI HAY VARIAS → MOSTRAR SELECTOR
                        // =================================================

                        if (terminalSeleccionada is null)
                        {
                            var selectorTerminal =
                                new MercadoPagoTerminalWindow(
                                    _viewModel.Total,
                                    terminales
                                );


                            var terminalConfirmada =
                                await selectorTerminal
                                    .ShowDialog<bool>(
                                        owner
                                    );


                            // Canceló selección de terminal.
                            // Regresamos al selector Efectivo/Tarjeta/etc.
                            if (!terminalConfirmada)
                                continue;


                            terminalSeleccionada =
                                selectorTerminal
                                    .TerminalSeleccionada;


                            if (terminalSeleccionada is null)
                            {
                                _viewModel.MensajeError =
                                    "No se seleccionó una terminal Mercado Pago.";

                                continue;
                            }
                        }


                        // =================================================
                        // 4. ABRIR PROCESAMIENTO DE MERCADO PAGO
                        // =================================================

                        var pagoMercadoPago =
                            new MercadoPagoPagoWindow(
                                _viewModel.IdVentaActual,
                                terminalSeleccionada.IdTpvBancaria,
                                _viewModel.Total,
                                terminalSeleccionada.NombreVisual
                            );


                        await pagoMercadoPago
                            .ShowDialog(
                                owner
                            );


                        // =================================================
                        // 5. OBTENER RESULTADO
                        // =================================================

                        var resultadoMercadoPago =
                            pagoMercadoPago.Resultado;


                        if (resultadoMercadoPago is null)
                        {
                            /*
                             * No tenemos confirmación de pago.
                             *
                             * MUY IMPORTANTE:
                             *
                             * No registramos ningún pago aquí.
                             * No finalizamos la venta.
                             * No volvemos a cobrar automáticamente.
                             */

                            _viewModel.MensajeError =
                                "No fue posible confirmar el resultado del pago con Mercado Pago.";

                            return;
                        }


                        // =================================================
                        // 6. REQUIERE CONCILIACIÓN
                        // =================================================

                        if (resultadoMercadoPago.RequiereConciliacion)
                        {
                            /*
                             * Puede existir un pago real en Mercado Pago
                             * cuyo estado local todavía no conocemos.
                             *
                             * NO permitir un segundo cobro automático.
                             */

                            _viewModel.MensajeError =
                                string.IsNullOrWhiteSpace(
                                    resultadoMercadoPago.Mensaje
                                )
                                    ? "El estado del pago requiere verificación. No vuelva a cobrar la venta."
                                    : resultadoMercadoPago.Mensaje;

                            return;
                        }


                        // =================================================
                        // 7. PAGO APROBADO Y REGISTRADO
                        // =================================================

                        if (
                            resultadoMercadoPago.PagoRegistrado
                            &&
                            resultadoMercadoPago.PagoCompleto
                        )
                        {
                            /*
                             * IMPORTANTE:
                             *
                             * estado_orden.php YA insertó el pago en:
                             *
                             * tr_pos_ventas_pagos
                             *
                             * Por lo tanto NO llamamos:
                             *
                             * RegistrarPagoAsync()
                             *
                             * ni RegistrarPagoEfectivoAsync().
                             *
                             * Solamente finalizamos la venta.
                             */

                            var finalizada =
                                await _viewModel
                                    .FinalizarVentaAsync();


                            if (!finalizada)
                            {
                                /*
                                 * El dinero YA fue cobrado.
                                 *
                                 * Si falla la finalización:
                                 *
                                 * NO volver a cobrar.
                                 *
                                 * CargarVentaActualAsync() podrá recuperar
                                 * posteriormente la venta pagada pendiente
                                 * de finalizar.
                                 */

                                return;
                            }


                            // =================================================
                            // VENTA TERMINADA
                            // =================================================

                            return;
                        }


                        // =================================================
                        // 8. CANCELADO / RECHAZADO / EXPIRADO
                        // =================================================

                        if (
                            resultadoMercadoPago.Cancelado
                            ||
                            resultadoMercadoPago.Rechazado
                            ||
                            resultadoMercadoPago.Expirado
                        )
                        {
                            /*
                             * No existe pago aplicado en NovaCore.
                             *
                             * La venta continúa BORRADOR.
                             *
                             * Regresamos al selector para permitir:
                             *
                             * - volver a intentar tarjeta;
                             * - elegir efectivo;
                             * - elegir crédito;
                             * - etc.
                             */

                            _viewModel.MensajeError =
                                resultadoMercadoPago.Mensaje;

                            continue;
                        }


                        // =================================================
                        // 9. ESTADO NO CONCLUYENTE
                        // =================================================

                        _viewModel.MensajeError =
                            string.IsNullOrWhiteSpace(
                                resultadoMercadoPago.Mensaje
                            )
                                ? "No fue posible determinar el estado final del pago."
                                : resultadoMercadoPago.Mensaje;

                        return;
                    }
                    catch (MercadoPagoException ex)
                    {
                        Console.WriteLine("====================================");
                        Console.WriteLine("ERROR MERCADO PAGO");
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine("====================================");
                        
                        _viewModel.MensajeError =
                            ex.Message;

                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("====================================");
                        Console.WriteLine("ERROR GENERAL MERCADO PAGO");
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine("====================================");
                        
                        _viewModel.MensajeError =
                            $"No fue posible procesar el pago con Mercado Pago: {ex.Message}";

                        return;
                    }
                }


                // =====================================================
                // OTROS MÉTODOS
                // =====================================================
                //
                // Tarjeta y transferencia se agregarán después.
                //
                // Si por alguna razón llega un método todavía no
                // implementado, volvemos a mostrar el selector.
                // =====================================================
            }
        }
        finally
        {
            /*
             * Si:
             *
             * - cerró Cobro
             * - canceló efectivo
             * - terminó correctamente
             * - terminó crédito
             *
             * siempre dejamos el POS preparado
             * para el siguiente escaneo.
             */

            EnfocarBuscadorProductos();
        }
    }


    // =========================================================
    // ENTREGA DE TICKET
    // =========================================================

    private async Task<string?> OnSolicitarEntregaTicket(
        TicketVenta ticket)
    {
        var owner =
            TopLevel.GetTopLevel(this)
            as Window;


        if (owner is null)
            return null;


        var ventana =
            new EntregaTicketWindow(
                ticket.FolioTicket,
                ticket.Total
            );


        var resultado =
            await ventana
                .ShowDialog<bool>(
                    owner
                );


        if (!resultado)
            return null;


        return
            ventana.OpcionSeleccionada;
    }


    // =========================================================
    // SOLICITAR TELÉFONO PARA WHATSAPP
    // =========================================================

    private async Task<string?>
        OnSolicitarTelefonoWhatsApp()
    {
        var owner =
            TopLevel.GetTopLevel(this)
            as Window;


        if (owner is null)
            return null;


        var ventana =
            new EnviarWhatsAppWindow();


        var resultado =
            await ventana
                .ShowDialog<bool>(
                    owner
                );


        if (!resultado)
            return null;


        return
            ventana.Telefono;
    }


    // =========================================================
    // CONFIRMACIÓN DE VENTA FINALIZADA
    // =========================================================

    private async Task OnSolicitarConfirmacionVentaFinalizada(
        string folioTicket)
    {
        var owner =
            TopLevel.GetTopLevel(this)
            as Window;


        if (owner is null)
            return;


        var ventana =
            new VentaFinalizadaWindow(
                folioTicket
            );


        await ventana
            .ShowDialog(
                owner
            );
    }


// =========================================================
// ENFOCAR BUSCADOR / ESCÁNER
// =========================================================

    private void EnfocarBuscadorProductos()
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (BuscadorProductosTextBox is null)
                    return;


                if (!BuscadorProductosTextBox.IsAttachedToVisualTree())
                    return;


                BuscadorProductosTextBox.Focus();


                BuscadorProductosTextBox.CaretIndex =
                    BuscadorProductosTextBox.Text?.Length
                    ?? 0;
            });
    }


    // =========================================================
    // DESVINCULAR EVENTOS
    // =========================================================

    private void OnDetachedFromVisualTree(
        object? sender,
        Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.SolicitarSeleccionPresentacion -=
                OnSolicitarSeleccionPresentacion;


            _viewModel.SolicitarEditarCantidad -=
                OnSolicitarEditarCantidad;


            _viewModel.SolicitarCobro -=
                OnSolicitarCobro;


            _viewModel.SolicitarEntregaTicket -=
                OnSolicitarEntregaTicket;


            _viewModel.SolicitarTelefonoWhatsApp -=
                OnSolicitarTelefonoWhatsApp;


            _viewModel.SolicitarConfirmacionVentaFinalizada -=
                OnSolicitarConfirmacionVentaFinalizada;
            
            _viewModel.SolicitarCambioPrecio -=
                OnSolicitarCambioPrecio;
        }


        _viewModel =
            null;
    }
}