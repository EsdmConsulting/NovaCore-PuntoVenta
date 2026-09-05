using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

using NovaCoreESDM.Models.Credito;
using NovaCoreESDM.Services.Printing;
using NovaCoreESDM.ViewModels.Credito;

namespace NovaCoreESDM.Views.Credito;

public partial class PagoCreditoView : UserControl
{
// =========================================================
// VIEWMODEL
// =========================================================

    private readonly PagoCreditoViewModel
        _viewModel;


// =========================================================
// IMPRESORA / CAJÓN
// =========================================================

    private readonly WindowsRawPrinterService
        _printerService =
            new();


// =========================================================
// FORMA DE PAGO EN PROCESO
// =========================================================

    private string
        _formaPagoEnProceso =
            string.Empty;
    
    


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public PagoCreditoView()
    {
        InitializeComponent();


        _viewModel =
            new PagoCreditoViewModel();


        DataContext =
            _viewModel;


        // =====================================================
        // ESCUCHAR PAGO SOLICITADO
        // =====================================================

        _viewModel
            .PagoSolicitado +=
            ViewModel_PagoSolicitado;


        // =====================================================
        // ESCUCHAR PAGO EXITOSO
        // =====================================================

        _viewModel
            .PagoAplicadoCorrectamente +=
            ViewModel_PagoAplicadoCorrectamente;
    }


// =========================================================
// PAGO SOLICITADO
// =========================================================

private async void ViewModel_PagoSolicitado(
    PagoCreditoPreparado pago)
{
    // =====================================================
    // OBTENER OWNER
    // =====================================================

    var owner =
        TopLevel.GetTopLevel(
            this
        ) as Window;


    if (owner is null)
        return;


    // =====================================================
    // ABRIR CONFIRMACIÓN
    // =====================================================

    var ventanaConfirmacion =
        new ConfirmarPagoCreditoWindow(
            pago.Cliente,
            pago.Total,
            pago.FormaPago,
            pago.Documentos
        );


    var confirmado =
        await ventanaConfirmacion
            .ShowDialog<bool>(
                owner
            );


    // =====================================================
    // CANCELÓ
    // =====================================================

    if (!confirmado)
        return;


    // =====================================================
    // ELEGIR FLUJO SEGÚN FORMA DE PAGO
    // =====================================================

    switch (
        pago.FormaPago
            .Trim()
            .ToUpperInvariant()
    )
    {
        // =================================================
        // EFECTIVO
        // =================================================

        case "EFECTIVO":
        {
            var ventanaEfectivo =
                new PagoCreditoEfectivoWindow(
                    pago.Total
                );


            var pagoConfirmado =
                await ventanaEfectivo
                    .ShowDialog<bool>(
                        owner
                    );


            // =================================================
            // CANCELÓ EN EFECTIVO
            // =================================================

            if (!pagoConfirmado)
                return;


// =================================================
// PREPARAR PAGO EN EFECTIVO
// =================================================

            pago.Request.FormaPago =
                "EFECTIVO";

            pago.Request.OrigenPago =
                "CAJA";


// =================================================
// RECORDAR FORMA DE PAGO
// =================================================

            _formaPagoEnProceso =
                "EFECTIVO";


// =================================================
// AHORA SÍ REGISTRAR EN BD
// =================================================

            await _viewModel
                .EjecutarPagoAsync(
                    pago.Request
                );


            break;
        }


        // =================================================
        // TARJETA
        // =================================================

        case "TARJETA":
        {
            // =================================================
            // ABRIR PAGO CON TARJETA
            // =================================================

            var ventanaTarjeta =
                new PagoCreditoTarjetaWindow(
                    pago.Total
                );


            var pagoConfirmado =
                await ventanaTarjeta
                    .ShowDialog<bool>(
                        owner
                    );


            // =================================================
            // CANCELÓ
            // =================================================

            if (!pagoConfirmado)
                return;


            // =================================================
            // COMPLETAR DATOS DEL REQUEST
            // =================================================

            pago.Request.FormaPago =
                "TARJETA";


            pago.Request.OrigenPago =
                "CAJA";


            pago.Request.Referencia =
                string.IsNullOrWhiteSpace(
                    ventanaTarjeta.Referencia
                )
                    ? null
                    : ventanaTarjeta.Referencia;


            // =================================================
            // REGISTRAR ABONO
            // =================================================
            
            _formaPagoEnProceso =
                "TARJETA";


            await _viewModel
                .EjecutarPagoAsync(
                    pago.Request
                );


            break;
        }


// =================================================
// TRANSFERENCIA
// =================================================

        case "TRANSFERENCIA":
        {
            // =================================================
            // ABRIR PAGO POR TRANSFERENCIA
            // =================================================

            var ventanaTransferencia =
                new PagoCreditoTransferenciaWindow(
                    pago.Total
                );


            var pagoConfirmado =
                await ventanaTransferencia
                    .ShowDialog<bool>(
                        owner
                    );


            // =================================================
            // CANCELÓ
            // =================================================

            if (!pagoConfirmado)
                return;


            // =================================================
            // COMPLETAR DATOS DEL REQUEST
            // =================================================

            pago.Request.FormaPago =
                "TRANSFERENCIA";


            pago.Request.OrigenPago =
                "TRANSFERENCIA";


            pago.Request.Referencia =
                ventanaTransferencia
                    .Referencia;


            // =================================================
            // REGISTRAR ABONO
            // =================================================
            
            _formaPagoEnProceso =
                "TRANSFERENCIA";

            await _viewModel
                .EjecutarPagoAsync(
                    pago.Request
                );


            break;
        }
        // =================================================
        // DESCONOCIDO
        // =================================================

        default:
        {
            _viewModel.MensajeError =
                "La forma de pago seleccionada no es válida.";

            break;
        }
    }
}


    // =========================================================
    // PAGO APLICADO CORRECTAMENTE
    // =========================================================

private async void ViewModel_PagoAplicadoCorrectamente(
    RegistrarAbonoCreditoResponse resultado)
{
    if (
        resultado.Data is null
    )
    {
        return;
    }


    // =====================================================
    // OBTENER OWNER
    // =====================================================

    var owner =
        TopLevel.GetTopLevel(
            this
        ) as Window;


    if (owner is null)
        return;


    // =====================================================
    // SI FUE EFECTIVO → ABRIR CAJÓN
    // =====================================================

    if (
        _formaPagoEnProceso
            .Equals(
                "EFECTIVO",
                StringComparison.OrdinalIgnoreCase
            )
    )
    {
        try
        {
            var cajonAbierto =
                await _printerService
                    .AbrirCajonAsync();


            if (!cajonAbierto)
            {
                Console.WriteLine(
                    "===================================="
                );

                Console.WriteLine(
                    "ABONO REGISTRADO, PERO EL CAJÓN NO CONFIRMÓ LA APERTURA."
                );

                Console.WriteLine(
                    "===================================="
                );
            }
        }
        catch (Exception ex)
        {
            /*
             * MUY IMPORTANTE:
             *
             * Si falla el cajón, NO intentamos registrar
             * nuevamente el abono.
             *
             * El dinero ya quedó registrado correctamente
             * en PostgreSQL.
             */

            Console.WriteLine(
                "===================================="
            );

            Console.WriteLine(
                "ABONO REGISTRADO, PERO FALLÓ LA APERTURA DEL CAJÓN:"
            );

            Console.WriteLine(
                ex.Message
            );

            Console.WriteLine(
                "===================================="
            );
        }
    }


    // =====================================================
    // ABRIR MODAL DE ÉXITO
    // =====================================================

    var ventana =
        new PagoCreditoExitosoWindow(
            resultado
        );


    await ventana
        .ShowDialog(
            owner
        );


    // =====================================================
    // LIMPIAR FORMA DE PAGO
    // =====================================================

    _formaPagoEnProceso =
        string.Empty;
}
    
    
    // =========================================================
// FORMATEAR MONTO AL SALIR DEL TEXTBOX
// =========================================================

    private void MontoAplicarTextBox_LostFocus(
        object? sender,
        RoutedEventArgs e)
    {
        if (
            sender is not TextBox textBox
            ||
            textBox.DataContext
                is not DocumentoPagoCreditoItem documento
        )
        {
            return;
        }


        documento
            .FormatearMontoAplicar();
    }
}