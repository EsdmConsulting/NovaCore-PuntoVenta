using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace NovaCoreESDM.Views.Cancelaciones;

// =============================================================
// CONFIRMAR CANCELACIÓN DE VENTA
// =============================================================
//
// Devuelve true solo si el administrador confirma.
//
// S     : confirma (tecla deliberada).
// Esc   : vuelve (botón con IsCancel).
// Enter : acciona el botón enfocado (por defecto, volver).
// =============================================================

public partial class ConfirmarCancelacionWindow : Window
{
    // Constructor sin parámetros requerido por el diseñador de Avalonia.
    public ConfirmarCancelacionWindow()
        : this(
            string.Empty,
            string.Empty,
            string.Empty)
    {
    }


    public ConfirmarCancelacionWindow(
        string folio,
        string total,
        string motivo)
    {
        InitializeComponent();

        TextoFolio.Text =
            folio;

        TextoTotal.Text =
            total;

        TextoMotivo.Text =
            motivo;

        MotivoBorder.IsVisible =
            !string.IsNullOrWhiteSpace(motivo);


        // Por seguridad el foco arranca en "Volver".
        Opened +=
            (_, _) =>
                Dispatcher.UIThread.Post(
                    () => BotonVolver.Focus(),
                    DispatcherPriority.Input);
    }


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


    private void Volver_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }


    private void Confirmar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }
}
