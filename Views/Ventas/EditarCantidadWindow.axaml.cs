using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Ventas;

/// <summary>
/// Cambiar la cantidad (piezas enteras) de una línea del carrito. Regresa
/// true con <see cref="CantidadSeleccionada"/>; false si se canceló.
/// Con los datos del producto muestra el antes, el cambio y el nuevo importe.
/// </summary>
public partial class EditarCantidadWindow : Window
{
    public int CantidadSeleccionada { get; private set; }

    private readonly int _cantidadActual;
    private readonly decimal _precio;
    private readonly int _maximo;
    private bool _ajustando;


    // Constructor sin parámetros para el diseñador.
    public EditarCantidadWindow() : this(1) { }


    /// <param name="maximo">Existencia que admite la línea (0 = sin límite conocido).</param>
    public EditarCantidadWindow(
        int cantidadActual,
        string? producto = null,
        string? presentacion = null,
        decimal precioUnitario = 0m,
        decimal maximo = 0m)
    {
        InitializeComponent();

        _cantidadActual = cantidadActual;
        _precio = precioUnitario;
        _maximo = maximo > 0m ? decimal.ToInt32(Math.Floor(maximo)) : 0;

        CantidadSeleccionada = cantidadActual;

        TextoProducto.Text = string.IsNullOrWhiteSpace(producto) ? "Producto" : producto;

        var piezas = cantidadActual == 1 ? "pieza" : "piezas";
        TextoCantidadActual.Text =
            (string.IsNullOrWhiteSpace(presentacion) ? string.Empty : $"{presentacion} · ") +
            $"Cantidad actual: {cantidadActual} {piezas}" +
            (precioUnitario > 0m ? $" · ${precioUnitario:N2} c/u" : string.Empty);

        TextoMaximo.Text = _maximo > 0 ? $"Existencia disponible: {_maximo}" : string.Empty;
        TextoMaximo.IsVisible = _maximo > 0;

        // Sin precio no hay importe que mostrar.
        PanelResumen.IsVisible = precioUnitario > 0m;

        Poner(cantidadActual);

        Opened += (_, _) =>
        {
            CantidadTextBox.Focus();
            CantidadTextBox.SelectAll();
        };
    }


    private bool Leer(out int cantidad) =>
        int.TryParse((CantidadTextBox.Text ?? string.Empty).Trim(), out cantidad);


    private void Poner(int cantidad)
    {
        cantidad = Math.Max(1, cantidad);

        _ajustando = true;
        CantidadTextBox.Text = cantidad.ToString();
        CantidadTextBox.CaretIndex = CantidadTextBox.Text.Length;
        _ajustando = false;

        Actualizar();
    }


    private void Actualizar()
    {
        var valida = Leer(out var cantidad) && cantidad > 0;
        var excede = valida && _maximo > 0 && cantidad > _maximo;

        BotonMenos.IsEnabled = valida && cantidad > 1;
        BotonMas.IsEnabled = true;

        var diferencia = valida ? cantidad - _cantidadActual : 0;

        TextoAntes.Text = $"{_cantidadActual} × ${_precio:N2} = ${_cantidadActual * _precio:N2}";
        TextoCambio.Text = diferencia switch
        {
            > 0 => $"+{diferencia} pz",
            < 0 => $"{diferencia} pz",
            _ => "Sin cambio"
        };
        TextoCambio.Foreground = Avalonia.Media.Brush.Parse(
            diferencia > 0 ? "#15803D" : diferencia < 0 ? "#B91C1C" : "#64748B");
        TextoNuevoImporte.Text = valida ? $"${cantidad * _precio:N2}" : "—";

        string? aviso = null;

        if (!valida)
            aviso = "Escribe una cantidad de piezas mayor a cero.";
        else if (excede)
            aviso = $"Ojo: la existencia disponible es {_maximo}. El sistema revisará si alcanza.";
        else if (diferencia < 0)
            aviso = "Si este producto no es lo último que escaneaste, se pedirá la autorización de un administrador para bajar la cantidad.";

        BannerAviso.IsVisible = aviso is not null;
        TextoAviso.Text = aviso ?? string.Empty;

        // Igual que antes: el servidor valida la existencia.
        BotonAceptar.IsEnabled = valida;
        TextoBotonAceptar.Text = valida
            ? $"Dejar {cantidad} {(cantidad == 1 ? "pieza" : "piezas")}"
            : "Aceptar";
    }


    private void Cantidad_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_ajustando)
            Actualizar();
    }

    private void Menos_Click(object? sender, RoutedEventArgs e)
    {
        Poner(Leer(out var actual) ? actual - 1 : _cantidadActual);
    }

    private void Mas_Click(object? sender, RoutedEventArgs e)
    {
        Poner(Leer(out var actual) ? actual + 1 : _cantidadActual);
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
        if (
            !int.TryParse(
                CantidadTextBox.Text,
                out var cantidad)
            ||
            cantidad <= 0
        )
        {
            return;
        }


        CantidadSeleccionada =
            cantidad;


        Close(true);
    }
}
