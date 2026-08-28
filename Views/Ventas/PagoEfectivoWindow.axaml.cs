using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class PagoEfectivoWindow : Window
{
    public decimal TotalVenta { get; }

    public decimal CantidadRecibida { get; private set; }

    public decimal Cambio { get; private set; }


    public PagoEfectivoWindow(
        decimal totalVenta)
    {
        InitializeComponent();

        TotalVenta =
            totalVenta;

        TextoTotal.Text =
            $"${totalVenta:N2}";


        Opened +=
            (_, _) =>
            {
                CantidadRecibidaTextBox.Focus();
                CantidadRecibidaTextBox.SelectAll();
            };
    }


    private void CantidadRecibida_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        TextoMensaje.Text =
            string.Empty;

        if (
            !decimal.TryParse(
                CantidadRecibidaTextBox.Text,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var recibido)
        )
        {
            CantidadRecibida =
                0;

            Cambio =
                0;

            TextoCambio.Text =
                "$0.00";

            ConfirmarButton.IsEnabled =
                false;

            return;
        }


        CantidadRecibida =
            recibido;


        if (recibido < TotalVenta)
        {
            Cambio =
                0;

            TextoCambio.Text =
                "$0.00";

            ConfirmarButton.IsEnabled =
                false;

            TextoMensaje.Text =
                $"Faltan ${(TotalVenta - recibido):N2}.";

            return;
        }


        Cambio =
            recibido -
            TotalVenta;


        TextoCambio.Text =
            $"${Cambio:N2}";


        ConfirmarButton.IsEnabled =
            true;
    }


    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }


    private void Confirmar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (CantidadRecibida < TotalVenta)
            return;


        Close(true);
    }
}