using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using NovaCoreESDM.ViewModels.Cancelaciones;

namespace NovaCoreESDM.Views.Cancelaciones;

public partial class CancelacionesView
    : UserControl
{
    // ============================================================
    // VIEWMODEL
    // ============================================================

    private CancelacionesViewModel? ViewModel =>
        DataContext as CancelacionesViewModel;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public CancelacionesView()
    {
        AvaloniaXamlLoader.Load(
            this);


        // La escena animada del banner solo se muestra cuando hay
        // espacio suficiente; en pantallas angostas se vería diminuta.
        var banner =
            this.FindControl<Border>("BannerCancelacion");

        var escena =
            this.FindControl<Control>("EscenaCancelacion");

        if (banner is not null && escena is not null)
        {
            banner.SizeChanged +=
                (_, e) =>
                    escena.IsVisible =
                        e.NewSize.Width >= 1050;
        }
    }


    // ============================================================
    // FILTRO DE ESTADO
    // ============================================================

    private void EstadoComboBox_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }


        if (sender is not ComboBox comboBox)
        {
            return;
        }


        if (comboBox.SelectedItem
            is not ComboBoxItem item)
        {
            return;
        }


        var estado =
            item.Content?
                .ToString()?
                .Trim()
            ?? string.Empty;


        /*
         * "Todos" significa no mandar filtro de estado.
         *
         * El PHP acepta:
         *
         * null / vacío
         * FINALIZADA
         * CANCELADA
         */

        ViewModel.EstadoSeleccionado =
            string.Equals(
                estado,
                "Todos",
                StringComparison.OrdinalIgnoreCase
            )
                ? string.Empty
                : estado;
    }


    // ============================================================
    // CANCELAR VENTA
    // ============================================================
    //
    // Aquí hacemos una confirmación adicional ANTES de ejecutar
    // la operación real.
    //
    // La cancelación puede modificar inventario, por lo que no
    // queremos ejecutarla únicamente con un clic accidental.
    // ============================================================

    private async void CancelarVenta_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }


        if (ViewModel.VentaDetalle is null)
        {
            return;
        }


        if (!ViewModel.PuedeCancelarVenta)
        {
            return;
        }

        var motivo = ViewModel.MotivoCancelacion?.Trim() ?? string.Empty;


        if (motivo.Length < CancelacionesViewModel.MinimoCaracteresMotivo)
        {
            ViewModel.MensajeError = $"El motivo de cancelación debe ser claro y contener al menos {CancelacionesViewModel.MinimoCaracteresMotivo} caracteres.";
            return;
        }

        if (string.IsNullOrWhiteSpace(
                ViewModel.MotivoCancelacion))
        {
            /*
             * Dejamos que el ViewModel muestre su validación.
             */

            if (ViewModel.CancelarVentaCommand.CanExecute(
                    null))
            {
                await ViewModel
                    .CancelarVentaCommand
                    .ExecuteAsync(
                        null);
            }

            return;
        }


        var ventanaPadre =
            TopLevel.GetTopLevel(
                this)
            as Window;


        if (ventanaPadre is null)
        {
            return;
        }


        var confirmacion =
            new ConfirmarCancelacionWindow(
                ViewModel.VentaDetalle.Folio ?? string.Empty,
                ViewModel.VentaDetalle.TextoTotal,
                motivo);


        var confirmado =
            await confirmacion
                .ShowDialog<bool>(
                    ventanaPadre);


        if (!confirmado)
        {
            return;
        }


        if (!ViewModel.CancelarVentaCommand
                .CanExecute(
                    null))
        {
            return;
        }


        await ViewModel
            .CancelarVentaCommand
            .ExecuteAsync(
                null);
    }
}