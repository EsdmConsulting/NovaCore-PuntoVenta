using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Devoluciones;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Tickets;
using NovaCoreESDM.Services.Cancelaciones;
using NovaCoreESDM.Services.Devoluciones;
using NovaCoreESDM.Services.Offline;
using NovaCoreESDM.Services.Printing;

namespace NovaCoreESDM.ViewModels.Devoluciones;

/// <summary>Un producto del ticket y cuánto se devuelve de él.</summary>
public partial class LineaDevolucionItem : ObservableObject
{
    public LineaVentaDevolucion Linea { get; }

    private readonly bool _ventaDevolvible;

    public LineaDevolucionItem(LineaVentaDevolucion linea, bool ventaDevolvible = true)
    {
        Linea = linea;
        _ventaDevolvible = ventaDevolvible;
    }

    public string Producto => Linea.Producto ?? string.Empty;
    public string Presentacion =>
        Linea.EsPromocion ? $"{Linea.Presentacion} · PROMOCIÓN" : Linea.Presentacion ?? string.Empty;

    public bool EsPorPeso =>
        string.Equals(Linea.UnidadMedida?.Trim(), "KG", StringComparison.OrdinalIgnoreCase);

    private static string Formato(decimal v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    public string TextoVendida => $"Vendidas {Formato(Linea.CantidadVendida)}";
    public string TextoDevuelta => Linea.CantidadDevuelta > 0 ? $"Ya devueltas {Formato(Linea.CantidadDevuelta)}" : string.Empty;
    public bool TieneDevueltas => Linea.CantidadDevuelta > 0;
    public string TextoDisponible => $"Se pueden devolver {Formato(Linea.CantidadDisponible)}";
    public string TextoPrecio => $"${Linea.PrecioUnitario:N2} c/u";
    public bool PuedeDevolverse => _ventaDevolvible && Linea.CantidadDisponible > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Importe), nameof(TextoImporte), nameof(TextoCantidad), nameof(Seleccionado))]
    private decimal _cantidad;

    public decimal Importe => Math.Round(Cantidad * Linea.PrecioUnitario, 2);
    public string TextoImporte => $"${Importe:N2}";
    public string TextoCantidad => Formato(Cantidad);
    public bool Seleccionado => Cantidad > 0;

    private decimal Paso => EsPorPeso ? 0.1m : 1m;

    [RelayCommand]
    private void Mas() => Cantidad = Math.Min(Linea.CantidadDisponible, Cantidad + Paso);

    [RelayCommand]
    private void Menos() => Cantidad = Math.Max(0m, Cantidad - Paso);

    [RelayCommand]
    private void Todo() => Cantidad = Linea.CantidadDisponible;
}


/// <summary>
/// CHG-0006: devolución en caja de productos de una venta pagada en
/// efectivo, con la autorización de un administrador (gerente/supervisor
/// cuando existan esos roles). El servidor la registra en CEDIS →
/// Devoluciones (la mercancía regresa al inventario) y saca el efectivo del
/// turno abierto, así el corte cuadra. Se abre el cajón y se imprime el
/// comprobante para el cliente.
/// </summary>
public partial class DevolucionesViewModel : ViewModelBase
{
    private readonly DevolucionesService _servicio = new();
    private readonly CancelacionesService _cancelaciones = new();

    /// <summary>Una devolución sin confirmar se reintenta con el mismo uuid (no se duplica).</summary>
    private string? _uuidPendiente;

    /// <summary>La asigna la vista: ventana de autorización (título, descripción) → nombre o null.</summary>
    public Func<string, string, Task<string?>>? SolicitarAutorizacionAsync { get; set; }

    /// <summary>La asigna la vista: confirmar antes de entregar el dinero.</summary>
    public Func<string, Task<bool>>? ConfirmarAsync { get; set; }

    public ObservableCollection<LineaDevolucionItem> Lineas { get; } = new();
    public ObservableCollection<MotivoDevolucion> Motivos { get; } = new();

    [ObservableProperty] private string _textoBusqueda = string.Empty;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayVenta), nameof(TextoVenta), nameof(TextoPagos), nameof(TextoFechaVenta))]
    private TicketDevolucionData? _ticket;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiereObservaciones), nameof(PuedeRegistrar))]
    private MotivoDevolucion? _motivoSeleccionado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeRegistrar))]
    private string _observaciones = string.Empty;

    public bool HayVenta => Ticket?.Venta is not null;
    public bool PuedeDevolver => Ticket?.PuedeDevolver == true;
    public bool HayBloqueo => HayVenta && !PuedeDevolver;
    public string MotivoBloqueo => Ticket?.MotivoBloqueo ?? string.Empty;
    public string Advertencia => Ticket?.Advertencia ?? string.Empty;
    public bool HayAdvertencia => HayVenta && PuedeDevolver && !string.IsNullOrWhiteSpace(Ticket?.Advertencia);

    public string TextoVenta =>
        Ticket?.Venta is { } v
            ? $"{(string.IsNullOrWhiteSpace(v.FolioTicket) ? v.Folio : v.FolioTicket)} · ${v.Total:N2}"
            : string.Empty;

    public string TextoFechaVenta =>
        Ticket?.Venta is { } v
            ? $"{v.Fecha} {v.Hora} · {v.Caja} · {v.Cajero}".Trim(' ', '·')
            : string.Empty;

    public string TextoPagos =>
        Ticket is null || Ticket.Pagos.Count == 0
            ? string.Empty
            : "Pagado con: " + string.Join(", ", Ticket.Pagos.Select(p => $"{p.Forma} ${p.Importe:N2}"));

    public decimal TotalADevolver => Lineas.Sum(l => l.Importe);
    public string TextoTotal => $"${TotalADevolver:N2}";
    public decimal PiezasADevolver => Lineas.Sum(l => l.Cantidad);

    public bool RequiereObservaciones => MotivoSeleccionado?.RequiereObservaciones == true;

    public bool PuedeRegistrar =>
        PuedeDevolver &&
        !EstaCargando &&
        PiezasADevolver > 0 &&
        MotivoSeleccionado is not null &&
        (!RequiereObservaciones || Observaciones.Trim().Length >= 5);

    public string TextoBotonDevolver =>
        PiezasADevolver > 0 ? $"Devolver {TextoTotal} en efectivo" : "Devolver";


    partial void OnEstaCargandoChanged(bool value) => OnPropertyChanged(nameof(PuedeRegistrar));

    partial void OnTicketChanged(TicketDevolucionData? value)
    {
        OnPropertyChanged(nameof(PuedeDevolver));
        OnPropertyChanged(nameof(HayBloqueo));
        OnPropertyChanged(nameof(MotivoBloqueo));
        OnPropertyChanged(nameof(Advertencia));
        OnPropertyChanged(nameof(HayAdvertencia));
    }


    private void Recalcular()
    {
        OnPropertyChanged(nameof(TotalADevolver));
        OnPropertyChanged(nameof(TextoTotal));
        OnPropertyChanged(nameof(PiezasADevolver));
        OnPropertyChanged(nameof(PuedeRegistrar));
        OnPropertyChanged(nameof(TextoBotonDevolver));
    }

    private void Linea_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LineaDevolucionItem.Cantidad))
            Recalcular();
    }


    private void Limpiar()
    {
        foreach (var linea in Lineas)
            linea.PropertyChanged -= Linea_PropertyChanged;

        Lineas.Clear();
        Motivos.Clear();
        Ticket = null;
        MotivoSeleccionado = null;
        Observaciones = string.Empty;
        _uuidPendiente = null;
        Recalcular();
    }


    // =========================================================
    // BUSCAR TICKET
    // =========================================================

    [RelayCommand]
    private async Task BuscarAsync()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        var folio = TextoBusqueda.Trim();

        if (folio.Length == 0)
        {
            MensajeError = "Escribe el folio del ticket (viene impreso abajo, en «Ticket:»).";
            return;
        }

        if (!ConexionService.EstaEnLinea)
        {
            MensajeError = "Sin conexión no se pueden hacer devoluciones: el servidor tiene que validar el ticket.";
            return;
        }

        Limpiar();
        EstaCargando = true;

        try
        {
            var r = await _servicio.BuscarTicketAsync(folio);

            if (r.Res != 1 || r.Data?.Venta is null)
            {
                MensajeError = string.IsNullOrWhiteSpace(r.Msg) ? "No se encontró el ticket." : r.Msg;
                return;
            }

            MostrarTicket(r.Data);
        }
        finally
        {
            EstaCargando = false;
        }
    }


    /// <summary>Llena la pantalla con el ticket encontrado.</summary>
    public void MostrarTicket(TicketDevolucionData data)
    {
        Limpiar();
        Ticket = data;

        foreach (var m in data.Motivos)
            Motivos.Add(m);

        // "Devolución del cliente" como motivo sugerido, si existe.
        MotivoSeleccionado =
            Motivos.FirstOrDefault(m => (m.Clave ?? string.Empty).Contains("CLIENTE", StringComparison.OrdinalIgnoreCase))
            ?? Motivos.FirstOrDefault();

        foreach (var l in data.Lineas)
        {
            var item = new LineaDevolucionItem(l, data.PuedeDevolver);
            item.PropertyChanged += Linea_PropertyChanged;
            Lineas.Add(item);
        }

        Recalcular();
    }


    [RelayCommand]
    private void NuevaBusqueda()
    {
        Limpiar();
        TextoBusqueda = string.Empty;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }


    // =========================================================
    // REGISTRAR DEVOLUCIÓN
    // =========================================================

    [RelayCommand]
    private async Task DevolverAsync()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (!PuedeRegistrar || Ticket?.Venta is null)
        {
            MensajeError = MotivoSeleccionado is null
                ? "Elige el motivo de la devolución."
                : RequiereObservaciones && Observaciones.Trim().Length < 5
                    ? "Este motivo necesita una observación."
                    : "Indica cuántas piezas se devuelven.";
            return;
        }

        if (!ConexionService.EstaEnLinea)
        {
            MensajeError = "Sin conexión no se pueden hacer devoluciones.";
            return;
        }

        if (!PosSession.TieneTurnoActivo)
        {
            MensajeError = "Se necesita un turno abierto en la caja: el efectivo sale del turno.";
            return;
        }

        var venta = Ticket.Venta;
        var total = TotalADevolver;
        var folioTicket = string.IsNullOrWhiteSpace(venta.FolioTicket) ? venta.Folio ?? string.Empty : venta.FolioTicket;

        if (ConfirmarAsync is not null &&
            !await ConfirmarAsync(
                $"Se devolverán {PiezasADevolver:0.###} pieza(s) del ticket {folioTicket} y se entregarán ${total:N2} en efectivo al cliente."))
            return;

        // 1. Autorización (el administrador que cobra no se la pide a sí mismo).
        var autorizo = PosSession.NombreUsuario ?? string.Empty;
        var usoVentana = false;

        if (!PosSession.EsAdministrador)
        {
            var nombre = SolicitarAutorizacionAsync is null
                ? null
                : await SolicitarAutorizacionAsync(
                    "Autorizar devolución",
                    $"Devolución del ticket {folioTicket}: ${total:N2} en efectivo. Se necesita la autorización de un administrador.");

            if (nombre is null)
            {
                MensajeError = "No se autorizó: no se hizo la devolución.";
                return;
            }

            autorizo = nombre;
            usoVentana = true;
        }

        EstaCargando = true;

        try
        {
            _uuidPendiente ??= Guid.NewGuid().ToString();

            var request = new RegistrarDevolucionRequest
            {
                IdVenta = venta.Id,
                IdCaja = PosSession.IdCaja,
                IdTurno = PosSession.IdTurno,
                UuidOperacion = _uuidPendiente,
                IdMotivo = MotivoSeleccionado!.Id,
                Observaciones = Observaciones.Trim(),
                Lineas = Lineas
                    .Where(l => l.Cantidad > 0)
                    .Select(l => new LineaDevolucionRequest { IdDetalle = l.Linea.IdDetalle, Cantidad = l.Cantidad })
                    .ToList()
            };

            var r = await _servicio.RegistrarAsync(request);

            if (r.Res != 1 || r.Data is null)
            {
                MensajeError = string.IsNullOrWhiteSpace(r.Msg) ? "No fue posible registrar la devolución." : r.Msg;

                // Rechazo definitivo: la próxima vez es otra operación.
                if (r.Codigo != "SIN_CONFIRMAR")
                    _uuidPendiente = null;

                return;
            }

            _uuidPendiente = null;

            var datos = r.Data;
            var importe = datos.ImporteReembolso > 0 || datos.Lineas.Count > 0 ? datos.ImporteReembolso : total;

            Console.WriteLine(
                $"Devolución {datos.FolioDevolucion} del ticket {folioTicket}: ${importe:N2} en efectivo, autorizó {autorizo}.");

            // 2. Cajón y comprobante (si fallan, la devolución ya quedó).
            await EntregarAsync(datos, venta, folioTicket, autorizo, importe);

            MensajeExito = datos.YaRegistrada
                ? $"La devolución {datos.FolioDevolucion} ya estaba registrada. Entrega ${importe:N2} solo si no se entregaron antes."
                : $"Devolución {datos.FolioDevolucion} registrada. Entrega ${importe:N2} en efectivo al cliente.";

            var exito = MensajeExito;
            Limpiar();
            TextoBusqueda = string.Empty;
            MensajeExito = exito;
        }
        finally
        {
            EstaCargando = false;

            if (usoVentana)
            {
                try { await _cancelaciones.CerrarAutorizacionAsync(); }
                catch { /* el servidor la vence sola */ }
            }
        }
    }


    private async Task EntregarAsync(DevolucionRegistrada datos, VentaDevolucion venta, string folioTicket, string autorizo, decimal importe)
    {
        try
        {
            await new WindowsRawPrinterService().AbrirCajonAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo abrir el cajón: {ex.Message}");
        }

        try
        {
            var productos = datos.Lineas.Count > 0
                ? datos.Lineas.Select(l => new TicketVentaDetalle
                {
                    Producto = l.Producto ?? string.Empty,
                    Presentacion = l.Presentacion ?? string.Empty,
                    Cantidad = l.Cantidad,
                    PrecioUnitario = l.PrecioUnitario,
                    Importe = l.Importe
                }).ToList()
                : Lineas.Where(l => l.Cantidad > 0).Select(l => new TicketVentaDetalle
                {
                    Producto = l.Producto,
                    Presentacion = l.Presentacion,
                    Cantidad = l.Cantidad,
                    PrecioUnitario = l.Linea.PrecioUnitario,
                    Importe = l.Importe
                }).ToList();

            await new TicketVentaService().ImprimirDevolucionAsync(new TicketVentaService.DatosDevolucion
            {
                FolioDevolucion = datos.FolioDevolucion ?? string.Empty,
                FolioTicket = folioTicket,
                FechaVenta = $"{venta.Fecha} {venta.Hora}".Trim(),
                Caja = PosSession.CodigoCaja ?? string.Empty,
                Cajero = PosSession.NombreUsuario ?? string.Empty,
                Autorizo = autorizo,
                Motivo = MotivoSeleccionado?.Descripcion ?? string.Empty,
                Importe = importe,
                Productos = productos
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Devolución registrada, pero no se pudo imprimir el comprobante: {ex.Message}");
        }
    }
}
