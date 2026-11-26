using System.Collections.ObjectModel;
using System.Linq;

namespace NovaCoreESDM.ViewModels.Ventas;

/// <summary>Un pago ya registrado en la venta (para mostrarlo al dividir el cobro).</summary>
public sealed class PagoRegistradoVenta
{
    public string Metodo { get; init; } = string.Empty;
    public decimal Importe { get; init; }

    public string Texto => $"✓ {Metodo} ${Importe:N2}";
}


/// <summary>
/// Cobro con varios métodos de pago: la ventana de cobro pregunta cuánto se
/// paga con cada método (por defecto, todo lo que falta) y lleva la lista de
/// los pagos ya registrados. Los pagos se registran uno por uno, igual que
/// antes (con y sin internet).
/// </summary>
public partial class VentasViewModel
{
    public ObservableCollection<PagoRegistradoVenta> PagosRegistrados { get; } = new();

    public string TextoPagosRegistrados =>
        string.Join("   ", PagosRegistrados.Select(p => p.Texto));


    /// <summary>Se llama cuando un método de pago terminó y subió lo pagado.</summary>
    public void AgregarPagoRegistrado(string metodo, decimal importe)
    {
        if (importe <= 0m)
            return;

        PagosRegistrados.Add(new PagoRegistradoVenta { Metodo = metodo, Importe = importe });
        OnPropertyChanged(nameof(TextoPagosRegistrados));
    }


    private void LimpiarPagosRegistrados()
    {
        if (PagosRegistrados.Count == 0)
            return;

        PagosRegistrados.Clear();
        OnPropertyChanged(nameof(TextoPagosRegistrados));
    }


    /// <summary>Nombre del método para la lista de pagos.</summary>
    public static string NombreMetodoPago(string? metodo) =>
        metodo switch
        {
            "EFECTIVO" => "Efectivo",
            "TARJETA" or "TARJETA_MERCADOPAGO" => "Tarjeta",
            "TRANSFERENCIA" => "Transferencia",
            "CREDITO" => "Crédito",
            _ => metodo ?? "Pago"
        };
}
