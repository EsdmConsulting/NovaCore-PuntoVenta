using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Productos;

namespace NovaCoreESDM.ViewModels.Ventas;

public partial class SeleccionarPresentacionViewModel : ViewModelBase
{
    public ProductoCatalogo Producto { get; }

    public ObservableCollection<PresentacionVentaItem> Presentaciones { get; }
        = new();

    [ObservableProperty]
    private PresentacionVentaItem? _presentacionSeleccionada;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    public bool PuedeAgregar =>
        PresentacionSeleccionada is not null;

    public event Action<PresentacionVentaItem>? PresentacionConfirmada;

    public SeleccionarPresentacionViewModel(
        ProductoCatalogo producto,
        string unidadCodigo)
    {
        Producto = producto;

        CargarPresentaciones(unidadCodigo);
    }

    private void CargarPresentaciones(
        string unidadCodigo)
    {
        Presentaciones.Clear();

        foreach (var presentacion in Producto.Presentaciones
                     .Where(p => p.Estatus != 3))
        {
            var precio =
                ObtenerPrecioVenta(
                    Producto,
                    presentacion,
                    unidadCodigo);

            Presentaciones.Add(
                new PresentacionVentaItem
                {
                    IdPresentacion =
                        presentacion.Id,

                    NombrePresentacion =
                        presentacion.NombrePresentacion,

                    UnidadMedida =
                        presentacion.UnidadMedida,

                    FactorConversion =
                        presentacion.FactorConversion,

                    CodigoBarras =
                        presentacion.CodigoBarras,

                    EsBase =
                        presentacion.EsBase,

                    Precio =
                        precio,

                    TienePrecio =
                        precio > 0
                });
        }
    }

    partial void OnPresentacionSeleccionadaChanged(
        PresentacionVentaItem? value)
    {
        MensajeError = string.Empty;

        OnPropertyChanged(nameof(PuedeAgregar));
    }

    [RelayCommand]
    private void Confirmar()
    {
        MensajeError = string.Empty;

        if (PresentacionSeleccionada is null)
        {
            MensajeError =
                "Selecciona una presentación.";

            return;
        }

        if (!PresentacionSeleccionada.TienePrecio)
        {
            MensajeError =
                "Esta presentación no tiene un precio de venta configurado.";

            return;
        }

        PresentacionConfirmada?.Invoke(
            PresentacionSeleccionada);
    }

    private static decimal ObtenerPrecioVenta(
        ProductoCatalogo producto,
        PresentacionProducto presentacion,
        string unidadCodigo)
    {
        /*
         * 1. Buscamos precio MENUDEO específico
         *    de la presentación.
         */
        var precioMenudeo =
            presentacion.Precios
                .Where(p =>
                    p.Estatus != 3 &&
                    p.Precio > 0)
                .OrderByDescending(p =>
                    p.FechaInicio)
                .FirstOrDefault(p =>
                    p.TipoPrecio.Equals(
                        "MENUDEO",
                        StringComparison.OrdinalIgnoreCase));

        if (precioMenudeo is not null)
        {
            return precioMenudeo.Precio;
        }

        /*
         * 2. Como respaldo temporal utilizamos
         *    precio de menudeo del punto de venta.
         *
         * Cuando afinemos backend podremos determinar
         * el precio efectivo por presentación + sucursal.
         */
        var disponibilidad =
            producto.Disponibilidad
                .FirstOrDefault(d =>
                    d.UnidadCodigo.Equals(
                        unidadCodigo,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    d.Estatus != 3);

        return disponibilidad?.PrecioMenudeo ?? 0;
    }
}

public partial class PresentacionVentaItem : ObservableObject
{
    public int IdPresentacion { get; set; }

    public string NombrePresentacion { get; set; }
        = string.Empty;

    public string UnidadMedida { get; set; }
        = string.Empty;

    public decimal FactorConversion { get; set; }

    public string CodigoBarras { get; set; }
        = string.Empty;

    public bool EsBase { get; set; }

    public decimal Precio { get; set; }

    public bool TienePrecio { get; set; }

    public string TextoPrecio =>
        TienePrecio
            ? $"${Precio:N2}"
            : "Sin precio";
}