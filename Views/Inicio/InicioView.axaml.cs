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