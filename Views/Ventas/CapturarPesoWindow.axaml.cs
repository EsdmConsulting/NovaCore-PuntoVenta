using System;
using System.Globalization;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace NovaCoreESDM.Views.Ventas;


/// <summary>
/// Ventana para capturar el peso de un producto vendido por KG / GRA.
/// Muestra el importe en tiempo real: PrecioUnitario × Peso.
/// </summary>
public partial class CapturarPesoWindow : Window
{
    // =========================================================
    // ESTADO
    // =========================================================

    private readonly decimal _precioUnitario;
    private readonly string _unidadMedida;


    /// <summary>
    /// Peso final capturado (en la unidad indicada al construir).
    /// Solo válido cuando el diálogo cierra con true.
    /// </summary>
    public decimal PesoIngresado { get; private set; }


    // =========================================================
    // CONTROLES
    // =========================================================

    private TextBox?    _pesoTextBox;
    private TextBlock?  _importeTextBlock;
    private TextBlock?  _detallePrecioTextBlock;
    private TextBlock?  _nombreProductoTextBlock;
    private TextBlock?  _unidadTextBlock;
    private TextBlock?  _labelPesoTextBlock;
    private Button?     _agregarButton;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public CapturarPesoWindow(
        string nombreProducto,
        string nombrePresentacion,
        string unidadMedida,
        decimal precioUnitario)
    {
        AvaloniaXamlLoader.Load(this);

        _precioUnitario = precioUnitario;
        _unidadMedida   = unidadMedida.ToUpperInvariant();


        // Controles
        _pesoTextBox            = this.FindControl<TextBox>("PesoTextBox");
        _importeTextBlock       = this.FindControl<TextBlock>("ImporteTextBlock");
        _detallePrecioTextBlock = this.FindControl<TextBlock>("DetallePrecioTextBlock");
        _nombreProductoTextBlock= this.FindControl<TextBlock>("NombreProductoTextBlock");
        _unidadTextBlock        = this.FindControl<TextBlock>("UnidadTextBlock");
        _labelPesoTextBlock     = this.FindControl<TextBlock>("LabelPesoTextBlock");
        _agregarButton          = this.FindControl<Button>("AgregarButton");


        // Encabezado
        if (_nombreProductoTextBlock != null)
            _nombreProductoTextBlock.Text = $"{nombreProducto} — {nombrePresentacion}";

        var esGramos = _unidadMedida is "GRA" or "GR";

        if (_unidadTextBlock != null)
            _unidadTextBlock.Text = esGramos
                ? "Ingresa el peso en gramos."
                : "Ingresa el peso en kilogramos.";

        if (_labelPesoTextBlock != null)
            _labelPesoTextBlock.Text = esGramos ? "Peso (g):" : "Peso (kg):";

        if (_detallePrecioTextBlock != null)
            _detallePrecioTextBlock.Text = esGramos
                ? $"Precio por gramo: ${precioUnitario:N4}"
                : $"Precio por kg: ${precioUnitario:N2}";


        // Enfocar el input al abrirse
        Opened += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _pesoTextBox?.Focus();
                _pesoTextBox?.SelectAll();
            }, DispatcherPriority.Input);
        };
    }


    // =========================================================
    // CAMBIO DE TEXTO — CALCULAR IMPORTE
    // =========================================================

    private void PesoTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var texto = _pesoTextBox?.Text?.Trim() ?? string.Empty;

        if (decimal.TryParse(
                texto,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var peso)
            && peso > 0m)
        {
            var importe = _precioUnitario * peso;

            if (_importeTextBlock != null)
                _importeTextBlock.Text = $"${importe:N2}";

            if (_agregarButton != null)
                _agregarButton.IsEnabled = true;
        }
        else
        {
            if (_importeTextBlock != null)
                _importeTextBlock.Text = "$0.00";

            if (_agregarButton != null)
                _agregarButton.IsEnabled = false;
        }
    }


    // =========================================================
    // TECLA ENTER — CONFIRMAR
    // =========================================================

    private void PesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            TryConfirmar();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(false);
        }
    }


    // =========================================================
    // BOTONES
    // =========================================================

    private void Agregar_Click(object? sender, RoutedEventArgs e)
        => TryConfirmar();

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
        => Close(false);


    // =========================================================
    // CONFIRMAR
    // =========================================================

    private void TryConfirmar()
    {
        var texto = _pesoTextBox?.Text?.Trim() ?? string.Empty;

        if (!decimal.TryParse(
                texto,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var peso)
            || peso <= 0m)
        {
            return;
        }

        PesoIngresado = peso;
        Close(true);
    }
}

