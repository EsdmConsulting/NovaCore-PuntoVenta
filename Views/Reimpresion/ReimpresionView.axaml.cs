using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NovaCoreESDM.Views.Reimpresion;

public partial class ReimpresionView : UserControl
{
    public ReimpresionView()
    {
        AvaloniaXamlLoader.Load(this);

        // La escena animada solo se muestra cuando el encabezado tiene espacio.
        var banner = this.FindControl<Border>("BannerReimpresion");
        var escena = this.FindControl<Control>("EscenaReimpresion");
        if (banner is not null && escena is not null)
        {
            banner.SizeChanged += (_, e) => escena.IsVisible = e.NewSize.Width >= 1050;
        }
    }
}
