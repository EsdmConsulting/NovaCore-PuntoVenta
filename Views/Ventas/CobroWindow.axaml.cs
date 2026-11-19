using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NovaCoreESDM.ViewModels.Ventas;

namespace NovaCoreESDM.Views.Ventas;

public partial class CobroWindow : Window
{
    // =========================================================
    // PROPIEDADES
    // =========================================================

    public decimal TotalVenta { get; }

    public string? MetodoSeleccionado { get; private set; }


    // =========================================================
    // VIEWMODEL DE LA VENTA
    // =========================================================

    private readonly VentasViewModel _viewModel;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public CobroWindow(
        decimal totalVenta,
        VentasViewModel viewModel)
    {
        InitializeComponent();


        TotalVenta =
            totalVenta;


        _viewModel =
            viewModel;


        if (_viewModel.TienePagosParciales)
        {
            TextoEtiquetaTotal.Text =
                "Saldo restante a pagar";

            TextoSubtituloTotal.Text =
                $"Total: ${_viewModel.Total:N2} | Pagado: ${_viewModel.MontoPagadoActual:N2}";

            TextoTotal.Text =
                $"${totalVenta:N2}";
        }
        else
        {
            TextoTotal.Text =
                $"${totalVenta:N2}";
        }


        // Arranca con Efectivo enfocado para cobrar solo con teclado.
        Opened +=
            (_, _) =>
                Dispatcher.UIThread.Post(
                    () => BotonEfectivo.Focus(NavigationMethod.Directional),
                    DispatcherPriority.Input);
    }


    // =========================================================
    // TECLADO
    // =========================================================
    //
    // 1-4   : elige el método directamente.
    // Flechas: se mueve entre las 4 tarjetas (cuadrícula 2 x 2).
    // Enter : lo resuelve el propio botón enfocado.
    // Esc   : lo resuelve el botón Cerrar (IsCancel).
    // =========================================================

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        var metodos =
            new[]
            {
                BotonEfectivo,
                BotonTarjeta,
                BotonTransferencia,
                BotonCredito
            };


        var indiceNumero =
            e.Key switch
            {
                Key.D1 or Key.NumPad1 => 0,
                Key.D2 or Key.NumPad2 => 1,
                Key.D3 or Key.NumPad3 => 2,
                Key.D4 or Key.NumPad4 => 3,
                _ => -1
            };


        if (indiceNumero >= 0)
        {
            e.Handled = true;

            metodos[indiceNumero].Focus(NavigationMethod.Directional);

            metodos[indiceNumero].RaiseEvent(
                new RoutedEventArgs(Button.ClickEvent));

            return;
        }


        var actual =
            Array.FindIndex(
                metodos,
                b => b.IsFocused);


        if (actual >= 0)
        {
            var destino =
                e.Key switch
                {
                    Key.Right when actual % 2 == 0 => actual + 1,
                    Key.Left when actual % 2 == 1 => actual - 1,
                    Key.Down when actual < 2 => actual + 2,
                    Key.Up when actual >= 2 => actual - 2,
                    _ => -1
                };


            if (destino >= 0)
            {
                e.Handled = true;

                metodos[destino].Focus(NavigationMethod.Directional);

                return;
            }
        }


        base.OnKeyDown(e);
    }


    // =========================================================
    // MENSAJES EN PANTALLA
    // =========================================================

    private void MostrarMensaje(
        string? mensaje,
        bool esInformativo = false)
    {
        BannerMensaje.IsVisible =
            !string.IsNullOrWhiteSpace(mensaje);

        BannerMensaje.Classes.Set(
            "info",
            esInformativo);

        TextoMensaje.Text =
            mensaje;
    }


    // =========================================================
    // EFECTIVO
    // =========================================================

    private void Efectivo_Click(
        object? sender,
        RoutedEventArgs e)
    {
        MetodoSeleccionado =
            "EFECTIVO";


        Close(true);
    }
    
    // =========================================================
    // TRANSFERENCIA
    // =========================================================
    
        private void Transferencia_Click(
        object? sender,
        RoutedEventArgs e)
    {
        MetodoSeleccionado =
            "TRANSFERENCIA";

        Close(true);
    }
    
    
    // =========================================================
// TARJETA / MERCADO PAGO
// =========================================================

    private void Tarjeta_Click(
        object? sender,
        RoutedEventArgs e)
    {
        MetodoSeleccionado =
            "TARJETA";


        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "PAGO CON TARJETA SELECCIONADO");

        Console.WriteLine(
            $"TOTAL: ${TotalVenta:N2}");

        Console.WriteLine(
            "====================================");


        // =====================================================
        // REGRESAR A VENTASVIEW
        // =====================================================
        //
        // IMPORTANTE:
        //
        // Aquí NO:
        //
        // - Creamos la orden Mercado Pago.
        // - Registramos un pago.
        // - Finalizamos la venta.
        //
        // Únicamente indicamos qué método seleccionó
        // el cajero.
        //
        // VentasView será quien:
        //
        // 1. Consulte las terminales disponibles.
        // 2. Permita seleccionar la Point.
        // 3. Abra MercadoPagoPagoWindow.
        // 4. Espere la confirmación real del pago.
        // 5. Finalice la venta únicamente después
        //    de que el backend confirme el pago.
        // =====================================================

        Close(true);
    }


    // =========================================================
    // CRÉDITO
    // =========================================================

    private async void Credito_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            // =====================================================
            // CONSULTAR CRÉDITO
            // =====================================================

            MostrarMensaje(
                "Consultando el crédito del cliente…",
                esInformativo: true);


            var resultado =
                await _viewModel
                    .ObtenerCreditoActualAsync();


            MostrarMensaje(null);


            if (
                resultado is null ||
                resultado.Data is null
            )
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "NO FUE POSIBLE CONSULTAR EL CRÉDITO");

                Console.WriteLine(
                    _viewModel.MensajeError);

                Console.WriteLine(
                    "====================================");

                MostrarMensaje(
                    string.IsNullOrWhiteSpace(_viewModel.MensajeError)
                        ? "No fue posible consultar el crédito del cliente."
                        : _viewModel.MensajeError);

                return;
            }


            // =====================================================
            // CRÉDITO NO AUTORIZADO
            // =====================================================

            if (!resultado.Data.PuedeVenderCredito)
            {
                var motivos =
                    resultado.Data
                        .MotivosBloqueo;


                _viewModel.MensajeError =
                    motivos.Count > 0
                        ? string.Join(
                            " ",
                            motivos
                        )
                        : "El cliente no tiene crédito disponible para esta venta.";


                MostrarMensaje(
                    _viewModel.MensajeError);

                return;
            }


            // =====================================================
            // VALIDAR CLIENTE
            // =====================================================

            var cliente =
                _viewModel
                    .ClientePosSeleccionado;


            if (cliente is null)
            {
                _viewModel.MensajeError =
                    "No existe un cliente seleccionado.";

                MostrarMensaje(
                    "Para vender a crédito, cierra esta ventana y elige un cliente (F9).");

                return;
            }


            // =====================================================
            // ABRIR CONFIRMACIÓN DE CRÉDITO
            // =====================================================

            var ventanaCredito =
                new CreditoVentaWindow(
                    cliente.Nombre,
                    cliente.Codigo,
                    cliente.Rfc,
                    TotalVenta,
                    resultado.Data
                );


            var confirmado =
                await ventanaCredito
                    .ShowDialog<bool>(
                        this
                    );


            // =====================================================
            // CANCELÓ CONFIRMACIÓN
            // =====================================================

            if (!confirmado)
                return;


            // =====================================================
            // CRÉDITO CONFIRMADO
            // =====================================================

            MetodoSeleccionado =
                "CREDITO";


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "VENTA A CRÉDITO CONFIRMADA POR EL CAJERO");

            Console.WriteLine(
                $"CLIENTE: {cliente.Nombre}");

            Console.WriteLine(
                $"TOTAL: ${TotalVenta:N2}");

            Console.WriteLine(
                "====================================");


            // =====================================================
            // REGRESAR A VENTASVIEW
            // =====================================================
            //
            // IMPORTANTE:
            //
            // Aquí todavía NO llamamos directamente a
            // FinalizarVentaCreditoAsync().
            //
            // Cerramos CobroWindow con true y VentasView
            // será quien ejecute la finalización real.
            //
            // Esto también evita dejar ventanas modales
            // abiertas mientras se procesa el ticket.
            // =====================================================

            Close(true);
        }
        catch (Exception ex)
        {
            _viewModel.MensajeError =
                $"No fue posible procesar la venta a crédito: {ex.Message}";

            MostrarMensaje(
                _viewModel.MensajeError);


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR VENTA A CRÉDITO:");

            Console.WriteLine(
                ex.ToString());

            Console.WriteLine(
                "====================================");
        }
    }


    // =========================================================
    // CERRAR
    // =========================================================

    private void Cerrar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}