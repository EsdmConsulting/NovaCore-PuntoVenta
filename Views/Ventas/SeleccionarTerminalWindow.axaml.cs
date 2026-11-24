using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace NovaCoreESDM.Views.Ventas;

/// <summary>
/// Al elegir Tarjeta: con qué terminal se cobra. Mercado Pago (conectada,
/// se confirma sola) o una terminal externa (se capturan los datos del
/// voucher). Para sumar otra terminal integrada (por ejemplo BBVA) se agrega
/// un botón en el XAML y un valor aquí.
/// </summary>
public partial class SeleccionarTerminalWindow : Window
{
    public const string MercadoPago = "MERCADOPAGO";
    public const string Externa = "EXTERNA";

    /// <summary>Terminal elegida (MercadoPago / Externa).</summary>
    public string? TerminalSeleccionada { get; private set; }


    // Constructor sin parámetros para el diseñador.
    public SeleccionarTerminalWindow() : this(0m, sinConexion: false) { }


    public SeleccionarTerminalWindow(decimal saldo, bool sinConexion)
    {
        InitializeComponent();

        TextoSaldo.Text = $"${saldo:N2}";

        // Sin internet Mercado Pago no se comunica con el punto de venta.
        if (sinConexion)
        {
            BotonMercadoPago.IsEnabled = false;
            TextoDetalleMercadoPago.Text = "No disponible sin internet";

            BannerAviso.IsVisible = true;
            TextoAviso.Text =
                "Sin conexión: cobra en la terminal y captura los datos del voucher con «Terminal externa». La venta se envía sola cuando regrese el internet.";
        }

        Opened += (_, _) =>
            Dispatcher.UIThread.Post(
                () => (BotonMercadoPago.IsEnabled ? BotonMercadoPago : BotonExterna)
                    .Focus(NavigationMethod.Directional),
                DispatcherPriority.Input);
    }


    protected override void OnKeyDown(KeyEventArgs e)
    {
        var boton = e.Key switch
        {
            Key.D1 or Key.NumPad1 => BotonMercadoPago,
            Key.D2 or Key.NumPad2 => BotonExterna,
            _ => null
        };

        if (boton is not null)
        {
            e.Handled = true;

            if (boton.IsEnabled)
                boton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            return;
        }

        if (e.Key is Key.Left or Key.Right)
        {
            var destino = BotonMercadoPago.IsFocused ? BotonExterna : BotonMercadoPago;

            if (destino.IsEnabled)
            {
                destino.Focus(NavigationMethod.Directional);
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }


    private void MercadoPago_Click(object? sender, RoutedEventArgs e) => Elegir(MercadoPago);

    private void Externa_Click(object? sender, RoutedEventArgs e) => Elegir(Externa);

    private void Regresar_Click(object? sender, RoutedEventArgs e) => Close(false);


    private void Elegir(string terminal)
    {
        TerminalSeleccionada = terminal;
        Close(true);
    }
}
