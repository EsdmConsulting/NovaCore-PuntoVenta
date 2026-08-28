using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class CobroWindow : Window
{
    public decimal TotalVenta { get; }

    public string? MetodoSeleccionado { get; private set; }


    public CobroWindow(
        decimal totalVenta)
    {
        InitializeComponent();

        TotalVenta =
            totalVenta;

        TextoTotal.Text =
            $"${totalVenta:N2}";
    }


    private void Efectivo_Click(
        object? sender,
        RoutedEventArgs e)
    {
        MetodoSeleccionado =
            "EFECTIVO";

        Close(true);
    }


    private void Cerrar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}