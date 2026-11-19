using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using NovaCoreESDM.ViewModels.Inicio;

namespace NovaCoreESDM.Views.Inicio;

public partial class InicioView : UserControl
{
    private bool
        _cargaInicialRealizada;


    public InicioView()
    {
        AvaloniaXamlLoader.Load(this);


        AttachedToVisualTree +=
            InicioView_AttachedToVisualTree;


        // La escena animada del banner solo se muestra cuando hay
        // espacio suficiente; en pantallas angostas se vería diminuta.
        var banner =
            this.FindControl<Border>("BannerBienvenida");

        var escena =
            this.FindControl<Control>("EscenaBanner");

        if (banner is not null && escena is not null)
        {
            banner.SizeChanged +=
                (_, e) =>
                    escena.IsVisible =
                        e.NewSize.Width >= 1050;
        }
    }


    // =========================================================
    // CARGAR DASHBOARD
    // =========================================================

    private async void InicioView_AttachedToVisualTree(
        object? sender,
        VisualTreeAttachmentEventArgs e)
    {
        if (_cargaInicialRealizada)
        {
            return;
        }


        if (DataContext is not InicioViewModel viewModel)
        {
            return;
        }


        _cargaInicialRealizada =
            true;


        await viewModel
            .CargarAsync();
    }
}