using Avalonia.Controls;
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