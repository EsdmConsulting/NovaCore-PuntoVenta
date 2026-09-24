using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
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


        TextoTotal.Text =
            $"${totalVenta:N2}";
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

            var resultado =
                await _viewModel
                    .ObtenerCreditoActualAsync();


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