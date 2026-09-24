using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using NovaCoreESDM.Models.Ventas;

namespace NovaCoreESDM.Models;

public partial class DetalleVenta : ObservableObject
{
    public int IdDetalle { get; set; }

    public Producto Producto { get; }


    // =========================================================
    // CANTIDAD
    // =========================================================

    [ObservableProperty]
    private int _cantidad = 1;


    // =========================================================
    // PRECIO ACTUAL DE LA LÍNEA
    // =========================================================
    //
    // Este precio ya NO depende directamente de Producto.Precio.
    //
    // Debe actualizarse con el precio que responde el backend.
    // =========================================================

    [ObservableProperty]
    private decimal _precioUnitario;


    // =========================================================
    // CONFIGURACIÓN DE PRECIO
    // =========================================================

    [ObservableProperty]
    private bool _precioAutomatico = true;

    [ObservableProperty]
    private string? _tipoPrecioMaximo;

    [ObservableProperty]
    private string? _tipoPrecioAplicado;

    [ObservableProperty]
    private int? _idPrecioAplicado;

    [ObservableProperty]
    private int? _idReglaAplicada;

    [ObservableProperty]
    private decimal _cantidadMinimaPrecio;


    // =========================================================
    // NIVELES DISPONIBLES
    // =========================================================

    [ObservableProperty]
    private List<TipoPrecioDisponible> _tiposDisponibles = new();

    [ObservableProperty]
    private TipoPrecioDisponible? _nivelAutomaticoPorCantidad;

    [ObservableProperty]
    private SiguienteNivelPrecio? _siguienteNivel;


    // =========================================================
    // INVENTARIO
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
         * solamente como valor temporal.
         *
         * En cuanto el backend responde al agregar la línea,
         * PrecioUnitario debe sustituirse por el precio real.
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
            ? "Automático"
            : "Manual";


    public string TextoSiguienteNivel
    {
        get
        {
            if (SiguienteNivel is null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(SiguienteNivel.Mensaje))
                return SiguienteNivel.Mensaje;

            if (string.IsNullOrWhiteSpace(SiguienteNivel.TipoPrecio))
                return string.Empty;

            return
                $"Faltan {SiguienteNivel.Faltan:N0} " +
                $"para {SiguienteNivel.TipoPrecio}";
        }
    }


    public bool TieneSiguienteNivel =>
        SiguienteNivel is not null;


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


    public string TextoExistencia =>
        MaximoLinea > 0
            ? $"Máximo: {MaximoLinea:N0}"
            : "Existencia disponible";


    // =========================================================
    // CAMBIOS DE PROPIEDADES
    // =========================================================

    partial void OnCantidadChanged(int value)
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


    partial void OnSiguienteNivelChanged(SiguienteNivelPrecio? value)
    {
        OnPropertyChanged(
            nameof(TextoSiguienteNivel));

        OnPropertyChanged(
            nameof(TieneSiguienteNivel));
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
}