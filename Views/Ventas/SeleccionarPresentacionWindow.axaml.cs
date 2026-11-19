using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Input;
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

        Opened +=
            OnOpened;

        // Enter agrega la presentación marcada aunque el foco esté en la
        // lista (la lista se queda con Enter y no llega al botón Agregar).
        AddHandler(
            KeyDownEvent,
            OnEnterPresionado,
            RoutingStrategies.Tunnel);
    }

    private void OnEnterPresionado(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter ||
            e.Source is Button)
            return;

        if (_viewModel.ConfirmarCommand.CanExecute(null))
        {
            e.Handled = true;

            _viewModel.ConfirmarCommand.Execute(null);
        }
    }

    // Al abrir: deja marcada la presentación base (o la primera) y pone
    // el foco en la lista, para elegir con las flechas sin usar el mouse.
    private void OnOpened(
        object? sender,
        System.EventArgs e)
    {
        var lista =
            this.FindControl<ListBox>("PresentacionesListBox");

        if (lista is null)
            return;

        if (_viewModel.PresentacionSeleccionada is null &&
            _viewModel.Presentaciones.Count > 0)
        {
            _viewModel.PresentacionSeleccionada =
                _viewModel.Presentaciones.FirstOrDefault(p => p.EsBase)
                ?? _viewModel.Presentaciones[0];
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                lista.Focus();

                (lista.ContainerFromItem(lista.SelectedItem!) as ListBoxItem)?.Focus();
            },
            DispatcherPriority.Input);
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

        Opened -=
            OnOpened;

        base.OnClosed(e);
    }
}