using System;
using System.Globalization;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using NovaCoreESDM.Services.Bascula;
using NovaCoreESDM.Services.Configuration;

namespace NovaCoreESDM.Views.Ventas;


/// <summary>
/// Ventana para capturar el peso de un producto vendido por KG / GRA.
/// Muestra el importe en tiempo real: PrecioUnitario × Peso.
///
/// Si el equipo tiene báscula configurada, el peso se llena solo
/// en vivo; el cajero puede escribirlo a mano cuando quiera y
/// volver a la báscula con «Usar báscula».
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
    private Border?     _estadoBasculaBorder;
    private TextBlock?  _estadoBasculaTextBlock;
    private Button?     _usarBasculaButton;


    // =========================================================
    // BÁSCULA
    // =========================================================

    private BasculaLector? _bascula;

    // true = el cajero escribió el peso; la báscula ya no lo pisa.
    private bool _modoManual;

    private bool _basculaConectada;
    private bool _pesoEstable;


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
        _estadoBasculaBorder    = this.FindControl<Border>("EstadoBasculaBorder");
        _estadoBasculaTextBlock = this.FindControl<TextBlock>("EstadoBasculaTextBlock");
        _usarBasculaButton      = this.FindControl<Button>("UsarBasculaButton");


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

            IniciarBascula();
        };

        Closed += (_, _) => DetenerBascula();
    }


    // =========================================================
    // BÁSCULA: CONECTAR / DESCONECTAR
    // =========================================================

    private void IniciarBascula()
    {
        var configuracion = new ScaleConfigurationService().ObtenerActiva();

        // Sin báscula configurada: la ventana funciona como siempre.
        if (configuracion is null)
            return;

        if (_estadoBasculaBorder != null)
            _estadoBasculaBorder.IsVisible = true;

        MostrarEstado("Conectando báscula…", string.Empty);

        _bascula = new BasculaLector(configuracion);

        _bascula.PesoLeido += lectura =>
            Dispatcher.UIThread.Post(() => AlLeerPeso(lectura));

        _bascula.Fallo += mensaje =>
            Dispatcher.UIThread.Post(() => AlFallarBascula(mensaje));

        if (!_bascula.Iniciar(out var error))
        {
            AlFallarBascula(error);
            return;
        }

        if (_unidadTextBlock != null)
            _unidadTextBlock.Text = "Coloca el producto en la báscula.";
    }


    private void DetenerBascula()
    {
        var bascula = _bascula;
        _bascula = null;

        if (bascula is null)
            return;

        // Cerrar el puerto sin trabar la ventana.
        Task.Run(bascula.Dispose);
    }


    private void AlLeerPeso(LecturaBascula lectura)
    {
        if (_bascula is null)
            return;

        _basculaConectada = true;
        _pesoEstable = lectura.Estable && lectura.PesoKg > 0m;

        if (_modoManual)
        {
            MostrarEstado(
                $"Báscula: {FormatearPeso(lectura.PesoKg)} {Sufijo}",
                string.Empty);
            return;
        }

        if (lectura.PesoKg <= 0m)
            MostrarEstado("Coloca el producto", string.Empty);
        else if (_pesoEstable)
            MostrarEstado("Peso estable", "estable");
        else
            MostrarEstado("Pesando…", "pesando");

        PonerPesoDeBascula(lectura.PesoKg);
    }


    private void AlFallarBascula(string mensaje)
    {
        _basculaConectada = false;
        _pesoEstable = false;

        Console.WriteLine($"BÁSCULA: {mensaje}");

        MostrarEstado("Báscula sin conexión", "sinConexion");

        if (_estadoBasculaBorder != null)
            ToolTip.SetTip(_estadoBasculaBorder, mensaje);

        if (_unidadTextBlock != null)
            _unidadTextBlock.Text = "La báscula no responde: escribe el peso.";

        _bascula?.Dispose();
        _bascula = null;
    }


    private void PonerPesoDeBascula(decimal pesoKg)
    {
        if (_pesoTextBox == null)
            return;

        var texto = pesoKg > 0m
            ? FormatearPeso(pesoKg)
            : string.Empty;

        if (_pesoTextBox.Text == texto)
            return;

        _pesoTextBox.Text = texto;
        _pesoTextBox.CaretIndex = texto.Length;
    }


    private bool EsGramos => _unidadMedida is "GRA" or "GR";

    private string Sufijo => EsGramos ? "g" : "kg";


    /// <summary>
    /// La báscula siempre lee en KG; si el producto se vende por
    /// gramos se muestra en gramos.
    /// </summary>
    private string FormatearPeso(decimal pesoKg) =>
        EsGramos
            ? Math.Round(pesoKg * 1000m, 0).ToString("0", CultureInfo.InvariantCulture)
            : pesoKg.ToString("0.000", CultureInfo.InvariantCulture);


    private void MostrarEstado(string texto, string clase)
    {
        if (_estadoBasculaTextBlock != null)
            _estadoBasculaTextBlock.Text = texto;

        if (_estadoBasculaBorder == null)
            return;

        _estadoBasculaBorder.Classes.Set("pesando", clase == "pesando");
        _estadoBasculaBorder.Classes.Set("estable", clase == "estable");
        _estadoBasculaBorder.Classes.Set("sinConexion", clase == "sinConexion");
    }


    /// <summary>
    /// El cajero toma el control del peso (lo escribe o usa un peso rápido).
    /// </summary>
    private void PasarAModoManual()
    {
        if (_modoManual || _bascula is null)
            return;

        _modoManual = true;

        if (_usarBasculaButton != null)
            _usarBasculaButton.IsVisible = true;

        MostrarEstado("Peso escrito a mano", string.Empty);
    }


    private void UsarBascula_Click(object? sender, RoutedEventArgs e)
    {
        _modoManual = false;

        if (_usarBasculaButton != null)
            _usarBasculaButton.IsVisible = false;

        PonerPesoDeBascula(0m);
        MostrarEstado("Coloca el producto", string.Empty);

        _pesoTextBox?.Focus();
    }


    // =========================================================
    // PESO RÁPIDO
    // =========================================================

    private void PesoRapido_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string valor } || _pesoTextBox == null)
            return;

        PasarAModoManual();

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
        // Al empezar a escribir encima del peso de la báscula,
        // se reemplaza en lugar de agregarle dígitos.
        if (!_modoManual && _bascula is not null && EsTeclaDePeso(e.Key))
        {
            PasarAModoManual();
            _pesoTextBox?.SelectAll();
        }

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

    private static bool EsTeclaDePeso(Key tecla) =>
        tecla is >= Key.D0 and <= Key.D9
            or >= Key.NumPad0 and <= Key.NumPad9
            or Key.OemPeriod or Key.Decimal or Key.OemComma
            or Key.Back or Key.Delete;


    private void TryConfirmar()
    {
        if (!IntentarLeerPeso(out var peso))
        {
            return;
        }

        // Con la báscula se espera a que el peso deje de moverse.
        if (!_modoManual && _basculaConectada && !_pesoEstable)
        {
            MostrarEstado("Espera a que el peso se estabilice", "pesando");
            return;
        }

        PesoIngresado = peso;
        Close(true);
    }
}

