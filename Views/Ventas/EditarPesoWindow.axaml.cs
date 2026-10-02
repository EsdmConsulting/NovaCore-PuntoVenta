using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

public partial class EditarPesoWindow : Window
{
    public decimal PesoSeleccionado { get; private set; }

    private readonly decimal _pesoActual;

    public EditarPesoWindow(decimal pesoActual)
    {
        InitializeComponent();

        _pesoActual = pesoActual;
        PesoSeleccionado = pesoActual;

        TextoPesoActual.Text =
            $"Peso actual: {pesoActual:0.###}";

        PesoTextBox.Text =
            pesoActual.ToString(
                "0.###",
                CultureInfo.CurrentCulture
            );

        Opened += (_, _) =>
        {
            PesoTextBox.Focus();
            PesoTextBox.SelectAll();
        };
    }

    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }

    private void Aceptar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var texto =
            PesoTextBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(texto))
            return;

        if (!decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var peso))
        {
            return;
        }

        if (peso <= 0m)
            return;

        PesoSeleccionado = peso;

        Close(true);
    }
}