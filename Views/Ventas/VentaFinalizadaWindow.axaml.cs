using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class VentaFinalizadaWindow : Window
{
    public VentaFinalizadaWindow(
        string folioTicket)
    {
        InitializeComponent();

        FolioTicketTextBlock.Text =
            folioTicket;
    }


    private void Aceptar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}