using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class PagoEfectivoWindow : Window
{
    public decimal TotalVenta { get; }

    public decimal CantidadRecibida { get; private set; }

    public decimal ImporteAplicado { get; private set; }

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

        if (sender is TextBox textBox)
        {
            var textoOriginal = textBox.Text ?? string.Empty;

            var textoLimpio = Regex.Replace(textoOriginal, "[^0-9.]", "");

            // 2. Controlar que no haya múltiples puntos
            int primerPunto = textoLimpio.IndexOf('.');
            if (primerPunto >= 0)
            {
                textoLimpio = textoLimpio.Substring(0, primerPunto + 1) + 
                              textoLimpio.Substring(primerPunto + 1).Replace(".", "");
            }

            if (textoOriginal != textoLimpio)
            {
                var posicionCursor = textBox.CaretIndex;
                textBox.Text = textoLimpio;
                textBox.CaretIndex = Math.Max(0, posicionCursor - (textoOriginal.Length - textoLimpio.Length));
                return; 
            }
        }

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

        if (recibido <= 0)
        {
            ImporteAplicado =
                0;

            Cambio =
                0;

            TextoCambio.Text =
                "$0.00";

            ConfirmarButton.IsEnabled =
                false;

            return;
        }

        if (recibido < TotalVenta)
        {
            ImporteAplicado =
                recibido;

            Cambio =
                0;

            TextoCambio.Text =
                "$0.00";

            ConfirmarButton.IsEnabled =
                true;

            TextoMensaje.Text =
                $"Pago parcial: se aplicarán ${recibido:N2} y quedará un saldo de ${(TotalVenta - recibido):N2}.";

            return;
        }

        ImporteAplicado =
            TotalVenta;

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
        if (CantidadRecibida <= 0)
            return;


        Close(true);
    }
}