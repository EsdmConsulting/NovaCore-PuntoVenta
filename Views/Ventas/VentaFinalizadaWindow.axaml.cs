using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
using System.Threading.Tasks;

namespace NovaCoreESDM.Views.Ventas;

public partial class VentaFinalizadaWindow : Window
{
    public VentaFinalizadaWindow(string folioTicket)
    {
        InitializeComponent();

        FolioTicketTextBlock.Text = folioTicket;
    }

    private void Aceptar_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void CopiarFolioButton_Click(object? sender, RoutedEventArgs e)
    {
        var textoFolio = FolioTicketTextBlock.Text;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (!string.IsNullOrWhiteSpace(textoFolio) && clipboard != null)
        {
            await clipboard.SetTextAsync(textoFolio);

            if (sender is Button botonCopiar)
            {
                botonCopiar.Content = "¡Copiado!";
                await Task.Delay(2000); 
                botonCopiar.Content = "Copiar";
            }
        }
    }
}