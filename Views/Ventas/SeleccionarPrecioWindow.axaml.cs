using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;

using CommunityToolkit.Mvvm.ComponentModel;

using NovaCoreESDM.Models;
using NovaCoreESDM.Services.Precios;



namespace NovaCoreESDM.Views.Ventas;

public partial class SeleccionarPrecioWindow : Window
{
    private readonly DetalleVenta _detalle;
    private readonly PreciosPresentacionService _preciosService;
    private readonly List<OpcionPrecioViewModel> _opciones = new();
    private bool _normalizandoPrecio;

    public SeleccionarPrecioWindow(DetalleVenta detalle)
    {
        InitializeComponent();

        _detalle = detalle;
        _preciosService = new PreciosPresentacionService();

        Opened += OnOpened;

        BtnCancelar.Click += (_, _) => Close(null);
        BtnAplicar.Click += OnAplicarClick;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await CargarConfiguracionAsync();
    }

    private async Task CargarConfiguracionAsync()
    {
        try
        {
            TxtError.IsVisible = false;
            TxtError.Text = string.Empty;
            BtnAplicar.IsEnabled = false;

            TxtProducto.Text = _detalle.Producto.Nombre;
            TxtPresentacion.Text = _detalle.Producto.NombrePresentacion;
            TxtCantidad.Text = $"Cantidad actual: {_detalle.Cantidad:0.###}";

            var resultado = await _preciosService.ObtenerConfiguracionAsync(
                _detalle.Producto.IdPresentacion,
                _detalle.Cantidad);

            if (resultado.Res != 1 || resultado.Data is null)
            {
                MostrarError(
                    string.IsNullOrWhiteSpace(resultado.Msg)
                        ? "No fue posible obtener los precios disponibles."
                        : resultado.Msg);
                return;
            }

            var data = resultado.Data;

            if (!data.ConfiguracionValida)
            {
                MostrarError("La configuración de precios de esta presentación no es válida.");
                return;
            }

            if (data.TiposDisponibles is null || data.TiposDisponibles.Count == 0)
            {
                MostrarError("Esta presentación no tiene precios disponibles para venta.");
                return;
            }

            _opciones.Clear();

            foreach (var tipo in data.TiposDisponibles)
            {
                _opciones.Add(new OpcionPrecioViewModel
                {
                    TipoPrecio = tipo.TipoPrecio,
                    Precio = tipo.Precio,
                    IsSelected = string.Equals(
                        _detalle.TipoPrecioAplicado,
                        tipo.TipoPrecio,
                        StringComparison.OrdinalIgnoreCase)
                });
            }

            // Si no hay un precio previamente seleccionado, se usa MENUDEO por defecto.
            if (!_opciones.Any(x => x.IsSelected))
            {
                var menudeo = _opciones.FirstOrDefault(
                    x => string.Equals(
                        x.TipoPrecio,
                        "MENUDEO",
                        StringComparison.OrdinalIgnoreCase));

                if (menudeo is not null)
                    menudeo.IsSelected = true;
            }

            ChkOtro.IsChecked = false;
            TxtPrecioOtro.Text = string.Empty;
            TxtPrecioOtro.IsEnabled = false;

            ListaPrecios.ItemsSource = _opciones;
            BtnAplicar.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MostrarError($"No fue posible cargar los precios: {ex.Message}");
        }
    }

    private void PrecioRadioButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton)
            return;

        var tipoPrecio = radioButton.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(tipoPrecio))
            return;

        ChkOtro.IsChecked = false;
        TxtPrecioOtro.IsEnabled = false;

        foreach (var opcion in _opciones)
        {
            opcion.IsSelected = string.Equals(
                opcion.TipoPrecio,
                tipoPrecio,
                StringComparison.OrdinalIgnoreCase);
        }

        Console.WriteLine($"PRECIO SELECCIONADO: {tipoPrecio}");
    }

    private void ChkOtro_Click(object? sender, RoutedEventArgs e)
    {
        var otroSeleccionado = ChkOtro.IsChecked == true;
        TxtPrecioOtro.IsEnabled = otroSeleccionado;

        if (otroSeleccionado)
        {
            foreach (var opcion in _opciones)
                opcion.IsSelected = false;

            TxtPrecioOtro.Focus();
        }
    }

    private void TxtPrecioOtro_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_normalizandoPrecio || TxtPrecioOtro.Text is null)
            return;

        var textoOriginal = TxtPrecioOtro.Text;
        var limpio = new System.Text.StringBuilder();
        var tienePunto = false;

        foreach (var caracter in textoOriginal)
        {
            if (char.IsDigit(caracter))
            {
                limpio.Append(caracter);
            }
            else if ((caracter == '.' || caracter == ',') && !tienePunto)
            {
                limpio.Append('.');
                tienePunto = true;
            }
        }

        var textoLimpio = limpio.ToString();
        if (!string.Equals(textoOriginal, textoLimpio, StringComparison.Ordinal))
        {
            _normalizandoPrecio = true;
            var posicion = TxtPrecioOtro.CaretIndex;
            TxtPrecioOtro.Text = textoLimpio;
            TxtPrecioOtro.CaretIndex = Math.Min(posicion, textoLimpio.Length);
            _normalizandoPrecio = false;
        }

        if (ChkOtro.IsChecked == true)
        {
            TxtError.IsVisible = false;
            TxtError.Text = string.Empty;
        }
    }

private void OnAplicarClick(object? sender, RoutedEventArgs e)
    {
        if (ChkOtro.IsChecked == true)
        {
            var textoPrecio = TxtPrecioOtro.Text?.Trim();

            if (!decimal.TryParse(
                    textoPrecio,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var precioOtro) ||
                precioOtro <= 0)
            {
                MostrarError("Ingresa un precio especial mayor que cero.");
                TxtPrecioOtro.Focus();
                return;
            }

            Close(new SeleccionPrecioResultado
            {
                TipoPrecio = "ESPECIAL",
                PrecioPersonalizado = precioOtro
            });
            return;
        }

        var seleccion = _opciones.FirstOrDefault(x => x.IsSelected);

        if (seleccion is null)
        {
            MostrarError("Selecciona un tipo de precio.");
            return;
        }

        if (string.IsNullOrWhiteSpace(seleccion.TipoPrecio))
        {
            MostrarError("El tipo de precio seleccionado no es válido.");
            return;
        }

        Close(new SeleccionPrecioResultado
        {
            TipoPrecio = seleccion.TipoPrecio,
            PrecioPersonalizado = null
        });
    }

    private void MostrarError(string mensaje)
    {
        TxtError.Text = mensaje;
        TxtError.IsVisible = true;
    }
}


public partial class OpcionPrecioViewModel : ObservableObject
{
    public string TipoPrecio { get; set; } = string.Empty;
    public decimal Precio { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    public string TextoPrecio => $"${Precio:N2}";
}

public class SeleccionPrecioResultado
{
    public string? TipoPrecio { get; set; }

    // Se llena únicamente cuando se selecciona "Otro".
    public decimal? PrecioPersonalizado { get; set; }
}
