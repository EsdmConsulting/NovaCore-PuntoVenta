using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using NovaCoreESDM.Models.Ventas;

namespace NovaCoreESDM.Models;

public partial class DetalleVenta : ObservableObject
{
    public int IdDetalle { get; set; }

    public Producto Producto { get; }
    
    // =========================================================
// UNIDAD DE MEDIDA
// =========================================================
//
// Unidad de medida de la presentación vendida.
//
// Ejemplos:
//
// PZA
// CAJ
// PQ
// BT
// KG
// GRA
//
// KG y GRA permiten captura de peso/cantidad fraccionaria.
// Las demás presentaciones utilizan cantidad entera.
// =========================================================

    [ObservableProperty]
    private string _unidadMedida = string.Empty;


    // =========================================================
    // CANTIDAD
    // =========================================================
    //
    // Decimal porque ahora el POS permite cantidades
    // fraccionarias.
    //
    // Ejemplos:
    //
    // 1
    // 0.500
    // 1.250
    // 2.750
    //
    // La cantidad NO modifica automáticamente el tipo
    // de precio aplicado.
    // =========================================================

    [ObservableProperty]
    private decimal _cantidad = 1m;


    // =========================================================
    // PRECIO ACTUAL DE LA LÍNEA
    // =========================================================
    //
    // Este es el precio unitario confirmado por el backend.
    //
    // Una línea nueva inicia siempre con MENUDEO.
    //
    // Posteriormente el cajero puede seleccionar otro tipo
    // de precio mediante el flujo autorizado correspondiente.
    //
    // Cambiar la cantidad NO cambia este precio automáticamente.
    // =========================================================

    [ObservableProperty]
    private decimal _precioUnitario;


    // =========================================================
    // CONFIGURACIÓN DE PRECIO
    // =========================================================
    //
    // Conservamos PrecioAutomatico y TipoPrecioMaximo
    // mientras el backend mantenga estos campos por
    // compatibilidad.
    //
    // Regla actual:
    //
    // PrecioAutomatico = true
    //     → MENUDEO
    //
    // PrecioAutomatico = false
    //     → precio seleccionado manualmente
    //
    // TipoPrecioAplicado contiene el tipo que realmente
    // está usando la línea.
    // =========================================================

    [ObservableProperty]
    private bool _precioAutomatico = true;

    [ObservableProperty]
    private string? _tipoPrecioMaximo;

    [ObservableProperty]
    private string? _tipoPrecioAplicado;

    [ObservableProperty]
    private int? _idPrecioAplicado;


    // =========================================================
    // TIPOS DE PRECIO DISPONIBLES
    // =========================================================
    //
    // La lista viene dinámicamente desde el backend.
    //
    // Ejemplos:
    //
    // MENUDEO
    // MAYOREO
    // DISTRIBUIDOR
    // ESPECIAL
    //
    // No se hardcodean aquí porque pueden existir nuevos
    // tipos de precio en el futuro.
    // =========================================================

    [ObservableProperty]
    private List<TipoPrecioDisponible> _tiposDisponibles = new();


    // =========================================================
    // INVENTARIO
    // =========================================================
    //
    // También son decimal porque el inventario puede manejar
    // cantidades fraccionarias.
    //
    // MaximoLinea representa la cantidad máxima que puede
    // quedar en esta línea respetando el inventario disponible.
    // =========================================================

    [ObservableProperty]
    private decimal _existenciaDisponible;

    [ObservableProperty]
    private decimal _maximoLinea;


    // =========================================================
    // ESTADO DE SINCRONIZACIÓN
    // =========================================================

    [ObservableProperty]
    private bool _estaSincronizando;

    [ObservableProperty]
    private bool _tieneErrorSincronizacion;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public DetalleVenta(
        Producto producto,
        int idDetalle = 0)
    {
        Producto = producto;
        IdDetalle = idDetalle;

        /*
         * Inicialmente utilizamos el precio que trae Producto
         * únicamente como valor temporal.
         *
         * En cuanto el backend confirma la línea,
         * PrecioUnitario se sustituye por el precio oficial.
         *
         * Para una línea nueva el backend debe devolver
         * siempre MENUDEO.
         */
        PrecioUnitario = producto.Precio;
    }


    // =========================================================
    // IMPORTES
    // =========================================================

    public decimal Importe =>
        PrecioUnitario * Cantidad;


    // =========================================================
    // INFORMACIÓN VISUAL DEL PRECIO
    // =========================================================

    public string TextoTipoPrecio
    {
        get
        {
            if (string.IsNullOrWhiteSpace(TipoPrecioAplicado))
                return "Precio";

            return TipoPrecioAplicado;
        }
    }


    public string TextoModoPrecio =>
        PrecioAutomatico
            ? "Menudeo"
            : "Manual";


    public bool TieneTiposPrecio =>
        TiposDisponibles is { Count: > 0 };


    // =========================================================
    // INVENTARIO
    // =========================================================

    public bool PuedeIncrementar =>
        !EstaSincronizando &&
        (
            MaximoLinea <= 0 ||
            Cantidad < MaximoLinea
        );
    
    public bool EsVentaPorPeso =>
        string.Equals(
            UnidadMedida,
            "KG",
            System.StringComparison.OrdinalIgnoreCase
        )
        ||
        string.Equals(
            UnidadMedida,
            "GRA",
            System.StringComparison.OrdinalIgnoreCase
        );


    public string TextoExistencia =>
        MaximoLinea > 0
            ? $"Máximo: {MaximoLinea:N3}"
            : "Existencia disponible";


    // =========================================================
    // CAMBIOS DE PROPIEDADES
    // =========================================================

    partial void OnCantidadChanged(decimal value)
    {
        OnPropertyChanged(
            nameof(Importe));

        OnPropertyChanged(
            nameof(PuedeIncrementar));
    }


    partial void OnPrecioUnitarioChanged(decimal value)
    {
        OnPropertyChanged(
            nameof(Importe));
    }


    partial void OnTipoPrecioAplicadoChanged(string? value)
    {
        OnPropertyChanged(
            nameof(TextoTipoPrecio));
    }


    partial void OnPrecioAutomaticoChanged(bool value)
    {
        OnPropertyChanged(
            nameof(TextoModoPrecio));
    }


    partial void OnTiposDisponiblesChanged(
        List<TipoPrecioDisponible> value)
    {
        OnPropertyChanged(
            nameof(TieneTiposPrecio));
    }


    partial void OnMaximoLineaChanged(decimal value)
    {
        OnPropertyChanged(
            nameof(PuedeIncrementar));

        OnPropertyChanged(
            nameof(TextoExistencia));
    }


    partial void OnEstaSincronizandoChanged(bool value)
    {
        OnPropertyChanged(
            nameof(PuedeIncrementar));
    }
    
    partial void OnUnidadMedidaChanged(string value)
    {
        OnPropertyChanged(
            nameof(EsVentaPorPeso));
    }
}