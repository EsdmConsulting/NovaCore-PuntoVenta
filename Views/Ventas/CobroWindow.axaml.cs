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

    public string? MetodoSeleccionado { get; set; }

    /// <summary>
    /// Cuánto se cobra con el método elegido (por defecto, todo lo que falta).
    /// Si es menos, el resto se cobra después con otro método (pago dividido).
    /// </summary>
    public decimal MontoACobrar { get; private set; }


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


        // =====================================================
        // COBRO DIVIDIDO
        // =====================================================

        MontoACobrar = totalVenta;
        PonerMonto(totalVenta);

        if (_viewModel.PagosRegistrados.Count > 0)
        {
            PanelPagosRegistrados.IsVisible = true;
            TextoPagosRegistrados.Text =
                $"Pagos registrados:   {_viewModel.TextoPagosRegistrados}";
        }
        else if (_viewModel.TienePagosParciales)
        {
            PanelPagosRegistrados.IsVisible = true;
            TextoPagosRegistrados.Text =
                $"Ya se pagaron ${_viewModel.MontoPagadoActual:N2} de esta venta.";
        }


        // Sin internet: efectivo, o tarjeta cobrada en la terminal y
        // capturada a mano (autorización del voucher).
        if (_viewModel.EsVentaSinConexion ||
            !NovaCoreESDM.Services.Offline.ConexionService.EstaEnLinea)
        {
            BotonTransferencia.IsEnabled = false;

            TextoDetalleTarjeta.Text = "Terminal externa (datos del voucher)";

            Opened += (_, _) =>
                MostrarMensaje(
                    "Sin conexión: efectivo, tarjeta (capturando el voucher) o crédito (con la información de crédito guardada en la caja). La venta se guarda en la caja y se envía sola cuando regrese el internet.",
                    esInformativo: true);
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
        // Escribiendo el monto: los números son del monto, no atajos.
        // Enter cobra con Efectivo (lo más común).
        if (CajaMonto.IsFocused)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                BotonEfectivo.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }

            base.OnKeyDown(e);
            return;
        }

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


        if (indiceNumero >= 0 && !metodos[indiceNumero].IsEnabled)
        {
            e.Handled = true;
            return;
        }

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
    // MONTO A COBRAR CON EL MÉTODO (COBRO DIVIDIDO)
    // =========================================================

    private bool _ajustandoMonto;

    private void PonerMonto(decimal monto)
    {
        monto = Math.Clamp(Math.Round(monto, 2), 0m, TotalVenta);

        _ajustandoMonto = true;
        CajaMonto.Text = monto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        _ajustandoMonto = false;

        Actualizar();
    }

    private bool LeerMonto(out decimal monto) =>
        decimal.TryParse(
            (CajaMonto.Text ?? string.Empty).Replace("$", string.Empty).Replace(",", string.Empty).Trim(),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out monto);

    private void Actualizar()
    {
        var valido = LeerMonto(out var monto) && monto > 0m && monto <= TotalVenta + 0.005m;

        if (valido)
            MontoACobrar = Math.Round(Math.Min(monto, TotalVenta), 2);

        var parcial = valido && MontoACobrar < TotalVenta;

        TextoAyudaMonto.Text =
            !valido
                ? $"Escribe un monto entre $0.01 y ${TotalVenta:N2}."
                : parcial
                    ? $"Pago dividido: se cobrarán ${MontoACobrar:N2} con el método que elijas y quedarán ${TotalVenta - MontoACobrar:N2} por cobrar."
                    : "Para dividir el pago, escribe una parte y elige el método. Lo que falte se cobra después con otro método.";

        TextoAyudaMonto.Foreground = Avalonia.Media.Brush.Parse(
            !valido ? "#B91C1C" : parcial ? "#15803D" : "#64748B");

        // El crédito es por toda la venta.
        BotonCredito.IsEnabled =
            _creditoPermitido && valido && !parcial && !_viewModel.TienePagosParciales;
    }

    private bool _creditoPermitido = true;

    private void Monto_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_ajustandoMonto)
            Actualizar();
    }

    private void MontoTodo_Click(object? sender, RoutedEventArgs e) => PonerMonto(TotalVenta);

    private void MontoMitad_Click(object? sender, RoutedEventArgs e) => PonerMonto(Math.Round(TotalVenta / 2m, 2));

    private void MontoFijo_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } &&
            decimal.TryParse(tag, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var monto))
            PonerMonto(Math.Min(monto, TotalVenta));
    }

    /// <summary>false = el monto no es válido (se muestra el motivo).</summary>
    private bool MontoValido()
    {
        if (LeerMonto(out var monto) && monto > 0m && monto <= TotalVenta + 0.005m)
        {
            MontoACobrar = Math.Round(Math.Min(monto, TotalVenta), 2);
            return true;
        }

        MostrarMensaje($"Escribe cuánto se cobra con este método (de $0.01 a ${TotalVenta:N2}).");
        CajaMonto.Focus();
        CajaMonto.SelectAll();
        return false;
    }


    // =========================================================
    // EFECTIVO
    // =========================================================

    private void Efectivo_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (!MontoValido())
            return;

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
        if (!MontoValido())
            return;

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
        if (!MontoValido())
            return;

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
        if (!MontoValido())
            return;

        if (MontoACobrar < TotalVenta || _viewModel.TienePagosParciales)
        {
            MostrarMensaje("El crédito es por el total de la venta: no se puede combinar con otros pagos.");
            return;
        }

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