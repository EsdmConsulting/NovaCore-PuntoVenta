using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class EntregaTicketWindow : Window
{
    public string OpcionSeleccionada { get; private set; }
        = string.Empty;


    public EntregaTicketWindow(
        string folioTicket,
        decimal total)
    {
        InitializeComponent();


        FolioTicketTextBlock.Text =
            folioTicket;


        TotalTextBlock.Text =
            $"${total:N2}";
    }


    private void Imprimir_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpcionSeleccionada =
            "IMPRIMIR";

        Close(true);
    }


    private void WhatsApp_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpcionSeleccionada =
            "WHATSAPP";

        Close(true);
    }


    private void SinTicket_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpcionSeleccionada =
            "SIN_TICKET";

        Close(true);
    }
}