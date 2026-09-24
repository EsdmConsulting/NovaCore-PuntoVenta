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
    private string _mensajeError =
        string.Empty;


    public bool PuedeAgregar =>
        PresentacionSeleccionada is not null;


    public event Action<PresentacionVentaItem>?
        PresentacionConfirmada;


    public SeleccionarPresentacionViewModel(
        ProductoCatalogo producto,
        string unidadCodigo)
    {
        Producto =
            producto;

        CargarPresentaciones(
            unidadCodigo);
    }


    // =========================================================
    // CARGAR PRESENTACIONES
    // =========================================================

    private void CargarPresentaciones(
        string unidadCodigo)
    {
        Presentaciones.Clear();


        foreach (
            var presentacion
            in Producto.Presentaciones
                .Where(p =>
                    p.Estatus != 3)
        )
        {
            /*
             * IMPORTANTE:
             *
             * Ya NO buscamos MENUDEO.
             *
             * Ya NO utilizamos:
             *
             * producto.Disponibilidad.PrecioMenudeo
             *
             * como respaldo.
             *
             * El precio comercial real será resuelto por
             * detalle.php cuando el usuario agregue la
             * presentación al carrito.
             *
             * Aquí solamente buscamos un valor visual
             * provisional si existe algún precio activo.
             *
             * Este valor NO determina el precio final.
             */


            var precioVisual =
                ObtenerPrecioVisual(
                    presentacion);


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
                        precioVisual,

                    /*
                     * Permitimos seleccionar la presentación.
                     *
                     * La validación definitiva del precio
                     * corresponde al backend.
                     */
                    TienePrecio =
                        true
                });
        }
    }


    // =========================================================
    // PRESENTACIÓN SELECCIONADA
    // =========================================================

    partial void OnPresentacionSeleccionadaChanged(
        PresentacionVentaItem? value)
    {
        MensajeError =
            string.Empty;


        OnPropertyChanged(
            nameof(PuedeAgregar));
    }


    // =========================================================
    // CONFIRMAR
    // =========================================================

    [RelayCommand]
    private void Confirmar()
    {
        MensajeError =
            string.Empty;


        if (PresentacionSeleccionada is null)
        {
            MensajeError =
                "Selecciona una presentación.";

            return;
        }


        /*
         * Ya NO rechazamos localmente una presentación
         * solamente porque el catálogo visual no encontró
         * un precio determinado.
         *
         * detalle.php será quien valide:
         *
         * - reglas activas;
         * - nivel base;
         * - precio vigente;
         * - configuración completa.
         */


        PresentacionConfirmada?.Invoke(
            PresentacionSeleccionada);
    }


    // =========================================================
    // PRECIO VISUAL PROVISIONAL
    // =========================================================

    private static decimal ObtenerPrecioVisual(
        PresentacionProducto presentacion)
    {
        /*
         * Este método NO resuelve el precio comercial.
         *
         * Solamente intenta evitar que el selector muestre
         * $0.00 cuando el catálogo ya trae algún precio
         * activo.
         *
         * No buscamos ningún nombre de tipo:
         *
         * MENUDEO
         * MAYOREO
         * DISTRIBUIDOR
         * etc.
         *
         * Tampoco asumimos una jerarquía.
         */


        var precio =
            presentacion.Precios?
                .Where(p =>
                    p.Estatus != 3
                    &&
                    p.Precio > 0)
                .OrderByDescending(
                    p => p.FechaInicio)
                .FirstOrDefault();


        return precio?.Precio
               ?? 0m;
    }
}


// =========================================================
// ITEM VISUAL DE PRESENTACIÓN
// =========================================================

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


    /*
     * Precio provisional para mostrar en el selector.
     *
     * El precio definitivo de la venta se guarda en:
     *
     * DetalleVenta.PrecioUnitario
     *
     * después de que responde detalle.php.
     */
    public decimal Precio { get; set; }


    /*
     * Se conserva para no romper los bindings actuales.
     *
     * Por ahora una presentación activa puede continuar
     * al backend y éste realiza la validación real.
     */
    public bool TienePrecio { get; set; }


    public string TextoPrecio =>
        Precio > 0
            ? $"Desde ${Precio:N2}"
            : "Precio al agregar";
}