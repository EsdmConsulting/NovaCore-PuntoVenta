using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Comun;

// =============================================================
// AVISO REUTILIZABLE
// =============================================================
//
// Ventana de advertencia con un solo botón "Entendido".
// Enter o Esc la cierran.
// =============================================================

public partial class AvisoWindow : Window
{
    // Constructor sin parámetros requerido por el diseñador de Avalonia.
    public AvisoWindow()
        : this(
            string.Empty,
            string.Empty)
    {
    }


    public AvisoWindow(
        string titulo,
        string mensaje)
    {
        InitializeComponent();

        Title =
            titulo;

        TextoTitulo.Text =
            titulo;

        TextoMensaje.Text =
            mensaje;
    }


    private void Entendido_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}
