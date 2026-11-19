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

        var sufijo = this.FindControl<TextBlock>("UnidadSufijoTextBlock");
        if (sufijo != null)
            sufijo.Text = esGramos ? "g" : "kg";


        // Pesos rápidos según la unidad
        var rapidos = esGramos
            ? new[] { ("100 g", "100"), ("250 g", "250"), ("500 g", "500"), ("1000 g", "1000") }
            : new[] { ("¼ kg", "0.250"), ("½ kg", "0.500"), ("1 kg", "1"), ("2 kg", "2") };

        for (var i = 0; i < rapidos.Length; i++)
        {
            var boton = this.FindControl<Button>($"Rapido{i + 1}Button");
            if (boton == null)
                continue;

            boton.Content = rapidos[i].Item1;
            boton.Tag = rapidos[i].Item2;
        }


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
    // PESO RÁPIDO
    // =========================================================

    private void PesoRapido_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string valor } || _pesoTextBox == null)
            return;

        _pesoTextBox.Text = valor;
        _pesoTextBox.Focus();
        _pesoTextBox.CaretIndex = valor.Length;
    }


    // =========================================================
    // LECTURA DEL PESO (acepta punto o coma decimal)
    // =========================================================

    private bool IntentarLeerPeso(out decimal peso)
    {
        var texto = (_pesoTextBox?.Text?.Trim() ?? string.Empty)
            .Replace(',', '.');

        return decimal.TryParse(
                   texto,
                   NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out peso)
               && peso > 0m;
    }


    // =========================================================
    // CAMBIO DE TEXTO — CALCULAR IMPORTE
    // =========================================================

    private void PesoTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IntentarLeerPeso(out var peso))
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
        if (!IntentarLeerPeso(out var peso))
        {
            return;
        }

        PesoIngresado = peso;
        Close(true);
    }
}

