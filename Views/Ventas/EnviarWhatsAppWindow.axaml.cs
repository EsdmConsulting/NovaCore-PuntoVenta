using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class EnviarWhatsAppWindow : Window
{
    public string Telefono { get; private set; }
        = string.Empty;


    public EnviarWhatsAppWindow()
    {
        InitializeComponent();

        Opened +=
            (_, _) =>
            {
                TelefonoTextBox.Focus();
            };
    }


    // =========================================================
    // CONTADOR DE DÍGITOS (solo visual)
    // =========================================================

    private void Telefono_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        var digitos =
            (TelefonoTextBox.Text ?? string.Empty)
                .Count(char.IsDigit);

        MensajeTextBlock.Text =
            string.Empty;

        if (digitos == 0)
        {
            IndicadorDigitos.Fill =
                new SolidColorBrush(Color.Parse("#CBD5E1"));

            TextoDigitos.Text =
                "Escribe los 10 dígitos del celular";
        }
        else if (digitos < 10)
        {
            IndicadorDigitos.Fill =
                new SolidColorBrush(Color.Parse("#F59E0B"));

            TextoDigitos.Text =
                $"{digitos} de 10 dígitos";
        }
        else
        {
            IndicadorDigitos.Fill =
                new SolidColorBrush(Color.Parse("#22C55E"));

            TextoDigitos.Text =
                digitos == 10
                    ? "Número completo · Presiona Enter para abrir WhatsApp"
                    : $"{digitos} dígitos (incluye lada internacional)";
        }
    }


    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }


    private void AbrirWhatsApp_Click(
        object? sender,
        RoutedEventArgs e)
    {
        MensajeTextBlock.Text =
            string.Empty;


        var telefono =
            TelefonoTextBox.Text?.Trim()
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(telefono))
        {
            MensajeTextBlock.Text =
                "Captura el número del cliente.";

            return;
        }


        Telefono =
            telefono;


        Close(true);
    }
}