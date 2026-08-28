using CommunityToolkit.Mvvm.ComponentModel;

namespace NovaCoreESDM.Models;

public partial class DetalleVenta : ObservableObject
{
    public int IdDetalle { get; set; }

    public Producto Producto { get; }

    [ObservableProperty]
    private int _cantidad = 1;

    [ObservableProperty]
    private decimal _existenciaDisponible;

    [ObservableProperty]
    private decimal _maximoLinea;

    [ObservableProperty]
    private bool _estaSincronizando;

    [ObservableProperty]
    private bool _tieneErrorSincronizacion;

    public DetalleVenta(
        Producto producto,
        int idDetalle = 0)
    {
        Producto = producto;
        IdDetalle = idDetalle;
    }

    public decimal Importe =>
        Producto.Precio * Cantidad;

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

    partial void OnCantidadChanged(int value)
    {
        OnPropertyChanged(
            nameof(Importe));

        OnPropertyChanged(
            nameof(PuedeIncrementar));
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