using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;

using NovaCoreESDM.Models;
using NovaCoreESDM.Models.Precios;
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
    // CARGAR CONFIGURACIÓN
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
                $"Cantidad actual: {_detalle.Cantidad}";


            // =================================================
            // CONSULTAR BACKEND
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
                        ? "No fue posible obtener la configuración de precios."
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
            // CREAR OPCIONES VISUALES
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

                        CantidadMinima =
                            tipo.CantidadMinima,

                        IsSelected =
                            !_detalle.PrecioAutomatico
                            &&
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


            ListaPrecios.ItemsSource =
                _opciones;


            // =================================================
            // ESTADO AUTOMÁTICO
            // =================================================

            RbAutomatico.IsChecked =
                _detalle.PrecioAutomatico;


            if (
                data.NivelAutomatico is not null
            )
            {
                TxtAutomaticoActual.Text =
                    $"Por cantidad aplicaría: " +
                    $"{data.NivelAutomatico.TipoPrecio} · " +
                    $"${data.NivelAutomatico.Precio:N2}";
            }
            else
            {
                TxtAutomaticoActual.Text =
                    string.Empty;
            }


            // =================================================
            // SIGUIENTE NIVEL
            // =================================================

            if (
                data.SiguienteNivel is not null
            )
            {
                PanelSiguienteNivel.IsVisible =
                    true;

                TxtSiguienteNivel.Text =
                    $"{data.SiguienteNivel.Mensaje} · " +
                    $"${data.SiguienteNivel.Precio:N2}";
            }
            else
            {
                PanelSiguienteNivel.IsVisible =
                    false;

                TxtSiguienteNivel.Text =
                    string.Empty;
            }


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
        // =====================================================
        // AUTOMÁTICO
        // =====================================================

        if (
            RbAutomatico.IsChecked ==
            true
        )
        {
            Close(
                new SeleccionPrecioResultado
                {
                    EsAutomatico =
                        true,

                    TipoPrecio =
                        null
                }
            );

            return;
        }


        // =====================================================
        // PRECIO MANUAL
        // =====================================================

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


        Close(
            new SeleccionPrecioResultado
            {
                EsAutomatico =
                    false,

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

        BtnAplicar.IsEnabled =
            false;
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


    public decimal CantidadMinima
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


    public string TextoCantidadMinima =>
        $"A partir de {CantidadMinima:N0}";
}


// =============================================================
// RESULTADO DE LA VENTANA
// =============================================================

public class SeleccionPrecioResultado
{
    public bool EsAutomatico
    {
        get;
        set;
    }


    public string? TipoPrecio
    {
        get;
        set;
    }
}