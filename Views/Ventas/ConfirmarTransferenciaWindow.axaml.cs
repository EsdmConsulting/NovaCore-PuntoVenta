using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

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


        // Por seguridad el foco arranca en "No, regresar": un Enter
        // de más (heredado de la pantalla anterior) nunca confirma.
        Opened +=
            (_, _) =>
                Dispatcher.UIThread.Post(
                    () => BotonRegresar.Focus(),
                    DispatcherPriority.Input);
    }


    // =========================================================
    // TECLADO
    // =========================================================
    //
    // S        : confirma (tecla deliberada).
    // Esc      : regresa (botón con IsCancel).
    // Enter    : acciona el botón enfocado (por defecto, regresar).
    // =========================================================

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        if (e.Key == Key.S)
        {
            e.Handled = true;

            Close(true);

            return;
        }

        base.OnKeyDown(e);
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