using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

/// <summary>
/// CHG-0005: cuántas piezas (o kilos) de la línea se regalan. Muestra cuánto
/// se cobra, cuánto es gratis y cuánto ahorra el cliente. No deja regalar
/// toda la venta: con total $0.00 no se puede cobrar.
/// </summary>
public partial class ProductoGratisWindow : Window
{
    private readonly decimal _cantidadLinea;
    private readonly decimal _precio;
    private readonly decimal _totalVenta;
    private readonly bool _porPeso;
    private bool _ajustando;

    /// <summary>Cantidad elegida (válida solo si la ventana regresa true).</summary>
    public decimal CantidadSeleccionada { get; private set; }


    // Constructor sin parámetros para el diseñador.
    public ProductoGratisWindow() : this("Producto", "Pieza", 3m, 15m, 45m, porPeso: false) { }


    public ProductoGratisWindow(
        string producto,
        string presentacion,
        decimal cantidadLinea,
        decimal precioUnitario,
        decimal totalVenta,
        bool porPeso)
    {
        InitializeComponent();

        _cantidadLinea = cantidadLinea;
        _precio = precioUnitario;
        _totalVenta = totalVenta;
        _porPeso = porPeso;

        var unidad = porPeso ? "kg" : (cantidadLinea == 1m ? "pieza" : "piezas");

        TextoProducto.Text = producto;
        TextoDetalleLinea.Text =
            $"{presentacion} · En la venta: {Formato(cantidadLinea)} {unidad} · ${precioUnitario:N2} c/u";

        TextoPregunta.Text = porPeso ? "¿Cuántos kilos son gratis?" : "¿Cuántas piezas son gratis?";
        TextoMaximo.Text = $"Máximo {Formato(cantidadLinea)} {(porPeso ? "kg" : unidad)}";

        // Lo normal es regalar 1 (compra 10, llévate 1).
        var inicial = porPeso ? cantidadLinea : Math.Min(1m, cantidadLinea);
        Poner(inicial);

        Opened += (_, _) =>
        {
            CajaCantidad.Focus();
            CajaCantidad.SelectAll();
        };
    }


    private static string Formato(decimal valor) =>
        valor.ToString("0.###", CultureInfo.InvariantCulture);

    private decimal Paso => _porPeso ? 0.1m : 1m;


    private bool Leer(out decimal cantidad) =>
        decimal.TryParse(
            (CajaCantidad.Text ?? string.Empty).Trim().Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out cantidad);


    private void Poner(decimal cantidad)
    {
        cantidad = Math.Clamp(Math.Round(cantidad, _porPeso ? 3 : 0), 0m, _cantidadLinea);

        _ajustando = true;
        CajaCantidad.Text = Formato(cantidad);
        CajaCantidad.CaretIndex = CajaCantidad.Text.Length;
        _ajustando = false;

        Actualizar();
    }


    private void Actualizar()
    {
        var valida = Leer(out var gratis) &&
                     gratis > 0m &&
                     gratis <= _cantidadLinea &&
                     (_porPeso || gratis == Math.Floor(gratis));

        if (!valida)
            gratis = 0m;

        var cobradas = _cantidadLinea - gratis;
        var ahorro = Math.Round(gratis * _precio, 2);
        var unidad = _porPeso ? "kg" : "pz";

        TextoSeCobran.Text = $"{Formato(cobradas)} {unidad} × ${_precio:N2} = ${cobradas * _precio:N2}";
        TextoGratis.Text = $"{Formato(gratis)} {unidad} × $0.00";
        TextoAhorro.Text = $"${ahorro:N2}";

        BotonMenos.IsEnabled = gratis - Paso > 0m;
        BotonMas.IsEnabled = gratis + Paso <= _cantidadLinea;

        // Toda la venta gratis: el total quedaría en $0.00 y no se puede cobrar.
        var ventaEnCero = valida && _totalVenta - ahorro <= 0.005m;

        string? aviso = null;

        if (!valida)
            aviso = _porPeso
                ? $"Escribe los kilos que se regalan (de 0.001 a {Formato(_cantidadLinea)})."
                : $"Escribe de 1 a {Formato(_cantidadLinea)} pieza(s).";
        else if (ventaEnCero)
            aviso = "La venta quedaría en $0.00 y no se puede cobrar. La promoción aplica sobre productos que sí se pagan " +
                    "(por ejemplo: compra 10 y llévate 1 gratis).";

        BannerAviso.IsVisible = aviso is not null;
        TextoAviso.Text = aviso ?? string.Empty;

        BotonRegalar.IsEnabled = valida && !ventaEnCero;
        TextoBotonRegalar.Text = valida
            ? $"Regalar {Formato(gratis)} {(_porPeso ? "kg" : gratis == 1m ? "pieza" : "piezas")}"
            : "Regalar";

        CantidadSeleccionada = valida ? gratis : 0m;
    }


    private void Cantidad_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_ajustando)
            Actualizar();
    }

    private void Menos_Click(object? sender, RoutedEventArgs e)
    {
        Leer(out var actual);
        Poner(actual - Paso);
    }

    private void Mas_Click(object? sender, RoutedEventArgs e)
    {
        Leer(out var actual);
        Poner(actual + Paso);
    }


    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Add or Key.OemPlus)
        {
            Mas_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Down or Key.Subtract or Key.OemMinus)
        {
            Menos_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }


    private void Regalar_Click(object? sender, RoutedEventArgs e)
    {
        Actualizar();

        if (BotonRegalar.IsEnabled && CantidadSeleccionada > 0m)
            Close(true);
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
}
