using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Credito;

public partial class PagoCreditoTransferenciaWindow : Window
{
    // =========================================================
    // DATOS
    // =========================================================

    public decimal TotalPago { get; }


    public string Referencia { get; private set; } =
        string.Empty;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public PagoCreditoTransferenciaWindow(
        decimal totalPago)
    {
        InitializeComponent();


        TotalPago =
            totalPago;


        TextoTotal.Text =
            $"${totalPago:N2}";


        Opened +=
            (_, _) =>
            {
                ReferenciaTextBox.Focus();
                ReferenciaTextBox.SelectAll();
            };
    }


    // =========================================================
    // CAMBIO DE REFERENCIA
    // =========================================================

    private void Referencia_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        Referencia =
            ReferenciaTextBox
                .Text?
                .Trim()
            ?? string.Empty;


        ConfirmarButton.IsEnabled =
            !string.IsNullOrWhiteSpace(
                Referencia
            );
    }


    // =========================================================
    // CANCELAR
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
        if (
            string.IsNullOrWhiteSpace(
                Referencia
            )
        )
        {
            return;
        }


        Close(true);
    }
}