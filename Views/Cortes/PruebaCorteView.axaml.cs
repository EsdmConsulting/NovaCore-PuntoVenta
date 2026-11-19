using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using NovaCoreESDM.ViewModels.Cortes;

namespace NovaCoreESDM.Views.Cortes;

public partial class PruebaCorteView : UserControl
{
    public PruebaCorteView()
    {
        AvaloniaXamlLoader.Load(this);

        // Las cajas (incluidas las del POS externo) se consultan al
        // mostrar la pantalla; solo la primera vez.
        AttachedToVisualTree += async (_, _) =>
        {
            if (DataContext is PruebaCorteViewModel viewModel)
            {
                await viewModel.CargarCajasAsync();
            }
        };

        // La escena animada del banner solo se muestra cuando hay
        // espacio suficiente; en pantallas angostas se vería diminuta.
        var banner = this.FindControl<Border>("BannerPruebaCorte");
        var escena = this.FindControl<Control>("EscenaPruebaCorte");

        if (banner is not null && escena is not null)
        {
            banner.SizeChanged += (_, e) =>
                escena.IsVisible = e.NewSize.Width >= 1050;
        }
    }
}
