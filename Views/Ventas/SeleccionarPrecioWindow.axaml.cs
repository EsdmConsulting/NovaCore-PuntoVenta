using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;

using NovaCoreESDM.Models;
using NovaCoreESDM.Services.Precios;

namespace NovaCoreESDM.Views.Ventas;

public partial class SeleccionarPrecioWindow : Window
{
    private readonly DetalleVenta _detalle;

    private readonly PreciosPresentacionService
        _preciosService;

    private readonly List<OpcionPrecioViewModel>
        _opciones = new();


    public SeleccionarPrecioWindow(
        DetalleVenta detalle)
    {
        InitializeComponent();

        _detalle =
            detalle;

        _preciosService =
            new PreciosPresentacionService();


        Opened +=
            OnOpened;

        BtnCancelar.Click +=
            (_, _) => Close(null);

        BtnAplicar.Click +=
            OnAplicarClick;
    }


    // =========================================================
    // ABRIR VENTANA
    // =========================================================

    private async void OnOpened(
        object? sender,
        EventArgs e)
    {
        await CargarConfiguracionAsync();
    }


    // =========================================================
    // CARGAR PRECIOS
    // =========================================================

    private async Task CargarConfiguracionAsync()
    {
        try
        {
            TxtError.IsVisible =
                false;

            TxtError.Text =
                string.Empty;

            BtnAplicar.IsEnabled =
                false;


            // =================================================
            // INFORMACIÓN VISUAL
            // =================================================

            TxtProducto.Text =
                _detalle.Producto.Nombre;

            TxtPresentacion.Text =
                _detalle.Producto.NombrePresentacion;

            TxtCantidad.Text =
                $"Cantidad actual: {_detalle.Cantidad:0.###}";


            // =================================================
            // CONSULTAR PRECIOS DE LA PRESENTACIÓN
            // =================================================

            var resultado =
                await _preciosService
                    .ObtenerConfiguracionAsync(
                        _detalle.Producto.IdPresentacion,
                        _detalle.Cantidad
                    );


            if (
                resultado.Res != 1 ||
                resultado.Data is null
            )
            {
                MostrarError(
                    string.IsNullOrWhiteSpace(resultado.Msg)
                        ? "No fue posible obtener los precios disponibles."
                        : resultado.Msg
                );

                return;
            }


            var data =
                resultado.Data;


            // =================================================
            // VALIDAR CONFIGURACIÓN
            // =================================================

            if (!data.ConfiguracionValida)
            {
                MostrarError(
                    "La configuración de precios de esta presentación no es válida."
                );

                return;
            }


            if (
                data.TiposDisponibles is null ||
                data.TiposDisponibles.Count == 0
            )
            {
                MostrarError(
                    "Esta presentación no tiene precios disponibles para venta."
                );

                return;
            }


            // =================================================
            // CREAR OPCIONES DINÁMICAS
            // =================================================

            _opciones.Clear();


            foreach (
                var tipo in
                data.TiposDisponibles
            )
            {
                var opcion =
                    new OpcionPrecioViewModel
                    {
                        TipoPrecio =
                            tipo.TipoPrecio,

                        Precio =
                            tipo.Precio,

                        IsSelected =
                            string.Equals(
                                _detalle.TipoPrecioAplicado,
                                tipo.TipoPrecio,
                                StringComparison.OrdinalIgnoreCase
                            )
                    };


                _opciones.Add(
                    opcion
                );
            }


            // =================================================
            // COMPATIBILIDAD CON VENTAS ANTIGUAS
            // =================================================
            //
            // Si por alguna razón la línea no trae todavía
            // TipoPrecioAplicado, intentamos seleccionar MENUDEO.
            //
            // Esto NO hace que el precio dependa de la cantidad.
            // MENUDEO únicamente es el precio predeterminado.
            // =================================================

            if (!_opciones.Any(x => x.IsSelected))
            {
                var menudeo =
                    _opciones.FirstOrDefault(
                        x => string.Equals(
                            x.TipoPrecio,
                            "MENUDEO",
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                if (menudeo is not null)
                {
                    menudeo.IsSelected =
                        true;
                }
            }


            ListaPrecios.ItemsSource =
                _opciones;


            BtnAplicar.IsEnabled =
                true;
        }
        catch (Exception ex)
        {
            MostrarError(
                $"No fue posible cargar los precios: {ex.Message}"
            );
        }
    }


    // =========================================================
    // APLICAR SELECCIÓN
    // =========================================================

    private void OnAplicarClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var seleccion =
            _opciones
                .FirstOrDefault(
                    x => x.IsSelected
                );


        if (seleccion is null)
        {
            MostrarError(
                "Selecciona un tipo de precio."
            );

            return;
        }


        if (
            string.IsNullOrWhiteSpace(
                seleccion.TipoPrecio
            )
        )
        {
            MostrarError(
                "El tipo de precio seleccionado no es válido."
            );

            return;
        }


        Close(
            new SeleccionPrecioResultado
            {
                TipoPrecio =
                    seleccion.TipoPrecio
            }
        );
    }


    // =========================================================
    // MOSTRAR ERROR
    // =========================================================

    private void MostrarError(
        string mensaje)
    {
        TxtError.Text =
            mensaje;

        TxtError.IsVisible =
            true;
    }
}


// =============================================================
// OPCIÓN VISUAL DE PRECIO
// =============================================================

public class OpcionPrecioViewModel
{
    public string TipoPrecio
    {
        get;
        set;
    } = string.Empty;


    public decimal Precio
    {
        get;
        set;
    }


    public bool IsSelected
    {
        get;
        set;
    }


    public string TextoPrecio =>
        $"${Precio:N2}";
}


// =============================================================
// RESULTADO DE LA VENTANA
// =============================================================

public class SeleccionPrecioResultado
{
    public string? TipoPrecio
    {
        get;
        set;
    }
}