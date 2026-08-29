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

namespace NovaCoreESDM.Views.Ventas;



public partial class VentasView : UserControl
{
    private readonly TerminalConfigurationService
        _terminalConfigurationService;


    private VentasViewModel?
        _viewModel;


    private bool
        _productosCargados;


    public VentasView()
    {
        AvaloniaXamlLoader.Load(this);


        _terminalConfigurationService =
            new TerminalConfigurationService();


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


            // =====================================================
            // SELECTOR DE MÉTODO DE PAGO
            // =====================================================

            var ventana =
                new CobroWindow(
                    _viewModel.Total
                );


            var resultado =
                await ventana
                    .ShowDialog<bool>(
                        owner
                    );


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


                if (!pagoConfirmado)
                    return;


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
        }


        _viewModel =
            null;
    }
}