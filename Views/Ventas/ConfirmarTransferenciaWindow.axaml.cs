using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class ConfirmarTransferenciaWindow : Window
{
    public decimal TotalVenta { get; }


    public ConfirmarTransferenciaWindow(
        decimal totalVenta)
    {
        InitializeComponent();


        TotalVenta =
            totalVenta;


        TextoTotal.Text =
            $"${totalVenta:N2}";
    }


    // =========================================================
    // REGRESAR
    // =========================================================

    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }


    // =========================================================
    // CONFIRMAR
    // =========================================================

    private void Confirmar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }
}