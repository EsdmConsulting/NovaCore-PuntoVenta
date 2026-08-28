using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NovaCoreESDM.Models.Productos;
using NovaCoreESDM.ViewModels.Ventas;

namespace NovaCoreESDM.Views.Ventas;

public partial class SeleccionarPresentacionWindow : Window
{
    private readonly SeleccionarPresentacionViewModel _viewModel;

    public PresentacionVentaItem? PresentacionSeleccionada { get; private set; }

    public SeleccionarPresentacionWindow(
        ProductoCatalogo producto,
        string unidadCodigo)
    {
        AvaloniaXamlLoader.Load(this);

        _viewModel =
            new SeleccionarPresentacionViewModel(
                producto,
                unidadCodigo);

        DataContext =
            _viewModel;

        _viewModel.PresentacionConfirmada +=
            OnPresentacionConfirmada;
    }

    private void OnPresentacionConfirmada(
        PresentacionVentaItem presentacion)
    {
        PresentacionSeleccionada =
            presentacion;

        Close(true);
    }

    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    protected override void OnClosed(
        System.EventArgs e)
    {
        _viewModel.PresentacionConfirmada -=
            OnPresentacionConfirmada;

        base.OnClosed(e);
    }
}