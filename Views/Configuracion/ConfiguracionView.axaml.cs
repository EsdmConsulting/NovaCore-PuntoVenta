using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NovaCoreESDM.ViewModels.Configuracion;

namespace NovaCoreESDM.Views.Configuracion;

public partial class ConfiguracionView : UserControl
{
    public ConfiguracionView()
    {
        AvaloniaXamlLoader.Load(this);

        // La escena animada solo se muestra cuando el encabezado tiene espacio.
        var banner = this.FindControl<Border>("BannerConfiguracion");
        var escena = this.FindControl<Control>("EscenaConfiguracion");
        if (banner is not null && escena is not null)
        {
            banner.SizeChanged += (_, e) => escena.IsVisible = e.NewSize.Width >= 1050;
        }

        // Prueba del escáner: se mide el tiempo entre letras, igual que en la venta.
        var cajaEscaner = this.FindControl<TextBox>("CajaPruebaEscaner");
        cajaEscaner?.AddHandler(InputElement.KeyDownEvent, PruebaEscaner_KeyDown, RoutingStrategies.Tunnel);

        // Ancho de papel guardado.
        DataContextChanged += (_, _) => (DataContext as ConfiguracionViewModel)?.CargarAnchoPapel();

        // Al salir de Configuración se libera el puerto de la báscula.
        DetachedFromVisualTree += (_, _) =>
            (DataContext as ConfiguracionViewModel)?.DetenerPruebaBascula();
    }


    // =========================================================
    // PRUEBA DEL ESCÁNER
    // =========================================================

    private DateTime _ultimaTeclaPrueba = DateTime.MinValue;
    private double _mayorEsperaPrueba;
    private int _teclasPrueba;

    private void PruebaEscaner_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox caja || DataContext is not ConfiguracionViewModel vm)
            return;

        var ahora = DateTime.UtcNow;
        var espera = (ahora - _ultimaTeclaPrueba).TotalMilliseconds;

        if (e.Key is Key.Enter or Key.Return)
        {
            e.Handled = true;

            vm.RegistrarPruebaEscaner(
                (caja.Text ?? string.Empty).Trim(),
                _teclasPrueba <= 1 ? 0 : Math.Max(_mayorEsperaPrueba, espera),
                terminoConEnter: true,
                NovaCoreESDM.Views.Ventas.VentasView.ScannerMaxDelayMs);

            caja.Text = string.Empty;
            _teclasPrueba = 0;
            _mayorEsperaPrueba = 0;
            _ultimaTeclaPrueba = DateTime.MinValue;
            return;
        }

        if (e.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt)
            return;

        // Una pausa larga = empieza otra lectura.
        if (espera > 1000)
        {
            _teclasPrueba = 0;
            _mayorEsperaPrueba = 0;
        }
        else if (_teclasPrueba > 0)
        {
            _mayorEsperaPrueba = Math.Max(_mayorEsperaPrueba, espera);
        }

        _teclasPrueba++;
        _ultimaTeclaPrueba = ahora;
    }
}
