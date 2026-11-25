using NovaCoreESDM.Models;

namespace NovaCoreESDM.ViewModels.Ventas;

/// <summary>
/// CHG-0004: cambiar el precio de un producto pide la contraseña de un
/// administrador (gerente/supervisor cuando existan esos roles). El servidor
/// la valida siempre y guarda el precio anterior, el nuevo y quién autorizó.
/// Sin internet no se puede cambiar el precio (no hay cómo validarla).
/// </summary>
public partial class VentasViewModel
{
    /// <summary>
    /// Revisa, antes de pedir la autorización, que el precio de la línea se
    /// pueda cambiar. false = no (MensajeError trae el motivo).
    /// </summary>
    public bool PuedeCambiarPrecio(DetalleVenta? detalle)
    {
        if (detalle is null)
            return false;

        if (BloquearSinConexion("cambiar el precio"))
            return false;

        if (detalle.EsPromocion)
        {
            MensajeError = "No se puede cambiar el precio de un producto de promoción.";
            return false;
        }

        if (detalle.EstaSincronizando)
        {
            MensajeError = "Espera un momento mientras se registra el producto.";
            return false;
        }

        if (IdVentaActual <= 0 || detalle.IdDetalle <= 0)
        {
            MensajeError = "No existe una venta válida para modificar el precio.";
            return false;
        }

        MensajeError = string.Empty;
        return true;
    }
}
