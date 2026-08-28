using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace NovaCoreESDM.Views.Turno;

public partial class ConfirmarAperturaWindow : Window
{
    public ConfirmarAperturaWindow(decimal fondoInicial)
    {
        AvaloniaXamlLoader.Load(this);

        var montoTextBlock =
            this.FindControl<TextBlock>("MontoTextBlock");

        if (montoTextBlock is not null)
        {
            montoTextBlock.Text = fondoInicial.ToString(
                "C2",
                CultureInfo.GetCultureInfo("es-MX")
            );
        }
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}