using Avalonia.Controls;
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

        // Al salir de Configuración se libera el puerto de la báscula.
        DetachedFromVisualTree += (_, _) =>
            (DataContext as ConfiguracionViewModel)?.DetenerPruebaBascula();
    }
}
