using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class PagoTransferenciaWindow : Window
{
    public decimal TotalVenta { get; }


    public PagoTransferenciaWindow(
        decimal totalVenta)
    {
        InitializeComponent();


        TotalVenta =
            totalVenta;


        TextoTotal.Text =
            $"${totalVenta:N2}";
    }


    // =========================================================
    // COPIAR CLABE
    // =========================================================

    private async void CopiarClabe_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var clipboard =
            TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is null ||
            string.IsNullOrWhiteSpace(TextoClabe.Text))
            return;

        await clipboard.SetTextAsync(
            TextoClabe.Text);

        if (sender is Button boton)
        {
            boton.Content =
                "¡Copiada!";

            await Task.Delay(2000);

            boton.Content =
                "Copiar";
        }
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
    // CONFIRMAR TRANSFERENCIA
    // =========================================================

    private async void Confirmar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        // Evitar doble clic.
        if (!ConfirmarButton.IsEnabled)
            return;


        // =====================================================
        // CONFIRMACIÓN FINAL
        // =====================================================

        var confirmacion =
            new ConfirmarTransferenciaWindow(
                TotalVenta
            );


        var owner =
            this;


        var confirmado =
            await confirmacion
                .ShowDialog<bool>(
                    owner
                );


        if (!confirmado)
            return;


        // =====================================================
        // MOSTRAR PROCESAMIENTO
        // =====================================================

        ConfirmarButton.IsEnabled =
            false;

        CancelarButton.IsEnabled =
            false;

        ProcesandoPanel.IsVisible =
            true;

        TextoMensaje.Text =
            string.Empty;

        TextoProcesando.Text =
            "Preparando registro de transferencia...";


        /*
         * Permitimos que Avalonia alcance a renderizar
         * el indicador antes de cerrar esta ventana.
         *
         * El registro real se realiza después en VentasView.
         */
        await Task.Delay(250);


        // =====================================================
        // CONFIRMADO
        // =====================================================

        Close(true);
    }
}