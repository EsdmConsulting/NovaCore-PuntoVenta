using System;
using System.Collections.Generic;
using System.Linq;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Venta hecha sin internet. Se guarda completa en la caja y se envía
/// al servidor (api/pos/ventas/sincronizar) cuando regresa la conexión.
/// </summary>
public sealed class VentaOffline
{
    public const string EstadoBorrador = "BORRADOR";
    public const string EstadoPendiente = "PENDIENTE";
    public const string EstadoEnviada = "ENVIADA";
    public const string EstadoRechazada = "RECHAZADA";

    /// <summary>Identificador único: con él el servidor nunca la duplica.</summary>
    public string Uuid { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Folio interno de la venta (SC-CAJA-AAMMDD-HHMMSS-XXXX), único aunque no haya internet.</summary>
    public string Folio { get; set; } = string.Empty;

    /// <summary>
    /// Folio de ticket impreso, con el mismo formato que los demás
    /// (UO-SERIE-00000000), apartado del consecutivo de la caja al cobrar.
    /// Vacío si la caja aún no conocía su serie (se imprime el folio interno).
    /// </summary>
    public string FolioTicket { get; set; } = string.Empty;
    public string SerieTicket { get; set; } = string.Empty;
    public long NumeroTicket { get; set; }

    public string Estado { get; set; } = EstadoBorrador;

    public int IdEmpresa { get; set; }
    public int IdUnidadOperativa { get; set; }
    public int IdCaja { get; set; }
    public int IdTurno { get; set; }
    public string CodigoCaja { get; set; } = string.Empty;

    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string NombreCajero { get; set; } = string.Empty;

    /// <summary>Cuándo se creó la venta (hora local de la caja).</summary>
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    /// <summary>Cuándo se cobró (hora local de la caja). Es la fecha que queda en el servidor.</summary>
    public DateTime? FechaCobro { get; set; }

    public List<LineaVentaOffline> Lineas { get; set; } = new();
    public List<PagoVentaOffline> Pagos { get; set; } = new();

    public decimal Recibido { get; set; }
    public decimal Cambio { get; set; }

    // --- Resultado del envío ---
    public int Intentos { get; set; }
    public string UltimoError { get; set; } = string.Empty;
    public DateTime? FechaEnvio { get; set; }
    public int? IdVentaServidor { get; set; }
    public string FolioTicketServidor { get; set; } = string.Empty;
    public List<string> Avisos { get; set; } = new();

    public decimal Total => Lineas.Sum(l => l.Importe);
    public decimal Pagado => Pagos.Sum(p => p.Importe);
}


public sealed class LineaVentaOffline
{
    public int IdProducto { get; set; }
    public int IdPresentacion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NombrePresentacion { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal FactorConversion { get; set; } = 1m;
    public decimal PrecioUnitario { get; set; }
    public string TipoPrecio { get; set; } = "MENUDEO";

    /// <summary>Igual que el servidor: cantidad × precio redondeado a 2 decimales.</summary>
    public decimal Importe => Math.Round(Cantidad * PrecioUnitario, 2, MidpointRounding.AwayFromZero);
}


public sealed class PagoVentaOffline
{
    public int IdFormaPago { get; set; }
    public decimal Importe { get; set; }
    public string Referencia { get; set; } = string.Empty;
    public string Autorizacion { get; set; } = string.Empty;
}
