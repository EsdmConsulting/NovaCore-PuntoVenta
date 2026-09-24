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
            new Window
            {
                Title =
                    "Confirmar cancelación",

                Width =
                    430,

                Height =
                    280,

                CanResize =
                    false,

                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };


        var botonVolver =
            new Button
            {
                Content =
                    "Volver",

                Width =
                    110,

                Height =
                    40,

                HorizontalContentAlignment =
                    Avalonia.Layout.HorizontalAlignment.Center
            };


        var botonCancelar =
            new Button
            {
                Content =
                    "Sí, cancelar venta",

                Width =
                    160,

                Height =
                    40,

                Background =
                    Avalonia.Media.Brushes.Firebrick,

                Foreground =
                    Avalonia.Media.Brushes.White,

                HorizontalContentAlignment =
                    Avalonia.Layout.HorizontalAlignment.Center
            };


        var resultado =
            false;


        botonVolver.Click +=
            (_, _) =>
            {
                confirmacion.Close(
                    false);
            };


        botonCancelar.Click +=
            (_, _) =>
            {
                resultado =
                    true;

                confirmacion.Close(
                    true);
            };


        confirmacion.Content =
            new Border
            {
                Padding =
                    new Avalonia.Thickness(
                        24),

                Child =
                    new StackPanel
                    {
                        Spacing =
                            16,

                        Children =
                        {
                            new TextBlock
                            {
                                Text =
                                    "¿Cancelar esta venta?",

                                FontSize =
                                    21,

                                FontWeight =
                                    Avalonia.Media.FontWeight.SemiBold,

                                Foreground =
                                    Avalonia.Media.Brushes.DarkRed
                            },

                            new TextBlock
                            {
                                Text =
                                    $"Folio: {ViewModel.VentaDetalle.Folio}",

                                FontSize =
                                    13,

                                FontWeight =
                                    Avalonia.Media.FontWeight.SemiBold
                            },

                            new TextBlock
                            {
                                Text =
                                    "Esta operación marcará la venta como CANCELADA y devolverá al inventario las cantidades vendidas.",

                                TextWrapping =
                                    Avalonia.Media.TextWrapping.Wrap,

                                Foreground =
                                    Avalonia.Media.Brushes.DimGray
                            },

                            new StackPanel
                            {
                                Orientation =
                                    Avalonia.Layout.Orientation.Horizontal,

                                Spacing =
                                    10,

                                HorizontalAlignment =
                                    Avalonia.Layout.HorizontalAlignment.Right,

                                Children =
                                {
                                    botonVolver,
                                    botonCancelar
                                }
                            }
                        }
                    }
            };


        var confirmado =
            await confirmacion
                .ShowDialog<bool>(
                    ventanaPadre);


        if (!confirmado ||
            !resultado)
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