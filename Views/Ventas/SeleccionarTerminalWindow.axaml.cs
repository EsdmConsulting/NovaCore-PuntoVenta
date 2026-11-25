using System.Linq;
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
    public const string Bancaria = "BANCARIA";

    /// <summary>Terminal elegida (MercadoPago / Externa / Bancaria).</summary>
    public string? TerminalSeleccionada { get; private set; }


    // Constructor sin parámetros para el diseñador.
    public SeleccionarTerminalWindow() : this(0m, sinConexion: false) { }


    /// <param name="hayTerminalesBancarias">
    /// El equipo tiene terminales configuradas (Configuración > Terminales de
    /// cobro): se muestra la opción 3.
    /// </param>
    public SeleccionarTerminalWindow(decimal saldo, bool sinConexion, bool hayTerminalesBancarias = false)
    {
        InitializeComponent();

        TextoSaldo.Text = $"${saldo:N2}";

        if (hayTerminalesBancarias)
        {
            BotonBancaria.IsVisible = true;
            PanelTerminales.Columns = 3;
            TextoTeclas.Text = "1-3";
            Width = 980;
            MinWidth = 820;

            // El cobro directo viaja por la red local, no por internet.
            if (sinConexion)
                TextoDetalleBancaria.Text = "Funciona sin internet: el pago se guarda en la caja";
        }

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
            Key.D3 or Key.NumPad3 when BotonBancaria.IsVisible => BotonBancaria,
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
            var botones = new[] { BotonMercadoPago, BotonExterna, BotonBancaria }
                .Where(b => b.IsVisible && b.IsEnabled)
                .ToList();

            if (botones.Count > 1)
            {
                var actual = botones.FindIndex(b => b.IsFocused);
                var paso = e.Key == Key.Right ? 1 : -1;
                var destino = botones[((actual < 0 ? 0 : actual) + paso + botones.Count) % botones.Count];

                destino.Focus(NavigationMethod.Directional);
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }


    private void MercadoPago_Click(object? sender, RoutedEventArgs e) => Elegir(MercadoPago);

    private void Externa_Click(object? sender, RoutedEventArgs e) => Elegir(Externa);

    private void Bancaria_Click(object? sender, RoutedEventArgs e) => Elegir(Bancaria);

    private void Regresar_Click(object? sender, RoutedEventArgs e) => Close(false);


    private void Elegir(string terminal)
    {
        TerminalSeleccionada = terminal;
        Close(true);
    }
}
