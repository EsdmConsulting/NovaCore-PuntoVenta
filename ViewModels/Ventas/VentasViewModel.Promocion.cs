using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models;

namespace NovaCoreESDM.ViewModels.Ventas;

/// <summary>
/// CHG-0005 (fase 1): producto gratis por promoción ("compra 10 y llévate el
/// 11"). Con la autorización de un administrador, N piezas de una línea pasan
/// a una línea aparte a $0.00 marcada PROMOCIÓN. El servidor valida la
/// autorización, separa la línea y guarda quién lo autorizó. Sin internet no
/// se puede (igual que el cambio de precio).
/// </summary>
public partial class VentasViewModel
{
    /// <summary>La atiende VentasView: autorización, cuántas piezas y registro.</summary>
    public event Action<DetalleVenta>? SolicitarProductoGratis;


    [RelayCommand]
    private void RegalarProducto(DetalleVenta? detalle)
    {
        if (!PuedeRegalarProducto(detalle))
            return;

        SolicitarProductoGratis?.Invoke(detalle!);
    }


    /// <summary>Se revisa antes de pedir la autorización. false = MensajeError trae el motivo.</summary>
    public bool PuedeRegalarProducto(DetalleVenta? detalle)
    {
        if (detalle is null)
            return false;

        if (BloquearSinConexion("regalar un producto"))
            return false;

        if (detalle.EsPromocion)
        {
            MensajeError = "Este producto ya es de promoción.";
            return false;
        }

        if (detalle.EstaSincronizando)
        {
            MensajeError = "Espera un momento mientras se registra el producto.";
            return false;
        }

        if (IdVentaActual <= 0 || detalle.IdDetalle <= 0)
        {
            MensajeError = "No existe una venta válida para regalar el producto.";
            return false;
        }

        if (MontoPagadoActual > 0)
        {
            MensajeError = "La venta ya tiene un pago registrado: termina de cobrarla antes de cambiar los productos.";
            return false;
        }

        MensajeError = string.Empty;
        return true;
    }


    /// <summary>Pasa <paramref name="cantidad"/> piezas de la línea a $0.00 (ya autorizado).</summary>
    public async Task<bool> RegistrarProductoGratisAsync(DetalleVenta detalle, decimal cantidad)
    {
        if (!PuedeRegalarProducto(detalle))
            return false;

        if (cantidad <= 0 || cantidad > detalle.Cantidad)
        {
            MensajeError = $"Indica de 1 a {detalle.Cantidad:0.###} pieza(s) para regalar.";
            return false;
        }

        detalle.EstaSincronizando = true;

        try
        {
            var resultado = await _ventasService.RegistrarProductoGratisAsync(
                IdVentaActual,
                detalle.IdDetalle,
                cantidad);

            if (resultado.Res != 1)
            {
                MensajeError = string.IsNullOrWhiteSpace(resultado.Msg)
                    ? "No fue posible regalar el producto."
                    : resultado.Msg;
                return false;
            }
        }
        catch (Exception ex)
        {
            MensajeError = $"No fue posible regalar el producto: {ex.Message}";
            return false;
        }
        finally
        {
            detalle.EstaSincronizando = false;
        }

        // El servidor separó la línea: se vuelve a leer la venta, conservando
        // lo quitado con autorización (CHG-0002) para el ticket.
        var cancelados = _productosCancelados.ToList();

        await CargarVentaActualAsync();

        _productosCancelados.Clear();
        _productosCancelados.AddRange(cancelados);

        MensajeError = string.Empty;
        return true;
    }
}
