using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class EditarCantidadWindow : Window
{
    public int CantidadSeleccionada { get; private set; }

    private readonly int _cantidadActual;

    public EditarCantidadWindow(
        int cantidadActual)
    {
        InitializeComponent();

        _cantidadActual =
            cantidadActual;

        CantidadSeleccionada =
            cantidadActual;


        TextoCantidadActual.Text =
            $"Cantidad actual: {cantidadActual}";

        CantidadTextBox.Text =
            cantidadActual.ToString();


        Opened +=
            (_, _) =>
            {
                CantidadTextBox.Focus();

                CantidadTextBox.SelectAll();
            };
    }


    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }


    private void Aceptar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (
            !int.TryParse(
                CantidadTextBox.Text,
                out var cantidad)
            ||
            cantidad <= 0
        )
        {
            return;
        }


        CantidadSeleccionada =
            cantidad;


        Close(true);
    }
}