using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NovaCoreESDM.Models;
using NovaCoreESDM.Models.Productos;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Tickets;
using NovaCoreESDM.Models.Ventas;
using NovaCoreESDM.Services.Offline;

namespace NovaCoreESDM.ViewModels.Ventas;

/// <summary>
/// Venta sin conexión: cuando no hay internet la venta se arma, se cobra
/// y se imprime en la caja. Cada cambio se guarda en disco (borrador) para
/// recuperarla tras un apagón; al cobrarla pasa a la cola de envío.
/// </summary>
public partial class VentasViewModel
{
    public const string MensajeSoloEfectivo =
        "Sin conexión solo se puede cobrar en efectivo, con tarjeta (capturando el voucher) o a crédito.";

    private VentaOffline? _ventaLocal;

    /// <summary>Cuándo se descargó el catálogo que se está usando.</summary>
    private DateTime _fechaCatalogo = DateTime.Now;

    private int _siguienteIdDetalleLocal = 1;

    /// <summary>
    /// Un pago al servidor falló por red: no se sabe si quedó registrado,
    /// así que la venta no se pasa a la caja (evita cobrar dos veces).
    /// </summary>
    private bool _pagoSinConfirmar;

    private bool _sinConexionInicializado;


    /// <summary>La venta actual se está haciendo sin conexión.</summary>
    public bool EsVentaSinConexion => _ventaLocal is not null;

    /// <summary>Hay una venta en curso (en el servidor o en la caja).</summary>
    public bool HayVentaActiva => IdVentaActual > 0 || _ventaLocal is not null;


    // =========================================================
    // AVISO DE CONEXIÓN (barra superior de Ventas)
    // =========================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarAvisoConexion))]
    [NotifyPropertyChangedFor(nameof(TextoAvisoConexion))]
    [NotifyPropertyChangedFor(nameof(DetalleAvisoConexion))]
    private bool _sinConexion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarAvisoConexion))]
    [NotifyPropertyChangedFor(nameof(TextoAvisoConexion))]
    [NotifyPropertyChangedFor(nameof(DetalleAvisoConexion))]
    private int _ventasPorEnviar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarAvisoConexion))]
    [NotifyPropertyChangedFor(nameof(TextoAvisoConexion))]
    [NotifyPropertyChangedFor(nameof(DetalleAvisoConexion))]
    private int _ventasRechazadas;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoAvisoConexion))]
    private bool _enviandoVentas;

    public bool MostrarAvisoConexion =>
        SinConexion || VentasPorEnviar > 0 || VentasRechazadas > 0;

    public string TextoAvisoConexion
    {
        get
        {
            if (SinConexion)
                return "Sin conexión · vendiendo sin internet";

            if (EnviandoVentas)
                return $"Enviando operaciones sin conexión… ({VentasPorEnviar})";

            if (VentasPorEnviar > 0)
                return $"{VentasPorEnviar} operación(es) sin conexión por enviar";

            return VentasRechazadas > 0
                ? $"{VentasRechazadas} operación(es) sin conexión no se pudieron registrar"
                : string.Empty;
        }
    }

    public string DetalleAvisoConexion
    {
        get
        {
            var partes = new List<string>();

            if (SinConexion)
                partes.Add("Efectivo, tarjeta (voucher) o crédito. Las ventas y abonos se guardan en esta caja y se envían solas al volver el internet.");

            if (VentasPorEnviar > 0)
                partes.Add($"{VentasPorEnviar} por enviar.");

            if (VentasRechazadas > 0)
                partes.Add($"{VentasRechazadas} rechazada(s) por el servidor: avisa al administrador.");

            return string.Join(" ", partes);
        }
    }


    private void InicializarSinConexion()
    {
        if (_sinConexionInicializado)
            return;

        _sinConexionInicializado = true;

        ConexionService.CambioEstado += _ =>
            Dispatcher.UIThread.Post(ActualizarEstadoConexion);

        VentasPendientesService.CambioCola += () =>
            Dispatcher.UIThread.Post(ActualizarEstadoConexion);

        ActualizarEstadoConexion();
    }


    private void ActualizarEstadoConexion()
    {
        SinConexion = !ConexionService.EstaEnLinea;
        EnviandoVentas = VentasPendientesService.EstaEnviando;

        try
        {
            VentasPorEnviar = VentasPendientesService.ContarPorEnviar() + AbonosPendientesService.ContarPorEnviar() + CorteDiferidoService.ContarPorEnviar();
            VentasRechazadas = VentasPendientesService.ContarRechazadas() + AbonosPendientesService.ContarRechazados() + CorteDiferidoService.ContarRechazados();
        }
        catch
        {
            // Lectura de disco fallida: se reintenta en el siguiente aviso.
        }

        IntentarPasarVentaAlServidor();
    }


    // =========================================================
    // CREAR / CONVERTIR / DESCARTAR
    // =========================================================

    /// <summary>Empieza una venta en la caja (sin servidor).</summary>
    private void IniciarVentaLocal()
    {
        _ventaLocal = new VentaOffline
        {
            Folio = GenerarFolioSinConexion(),
            IdEmpresa = PosSession.IdEmpresa,
            IdUnidadOperativa = PosSession.IdUnidadOperativa,
            IdCaja = PosSession.IdCaja,
            IdTurno = PosSession.IdTurno,
            CodigoCaja = PosSession.CodigoCaja,
            IdUsuario = PosSession.IdUsuario,
            Usuario = PosSession.Usuario,
            NombreCajero = PosSession.NombreUsuario
        };

        // La venta local no cambia IdVentaActual (siempre 0), así que la marca
        // "venta finalizada" de la venta anterior se limpia aquí. Sin esto el
        // botón Cobrar no hacía nada después de una venta cobrada.
        VentaFinalizadaCorrectamente = false;

        IdVentaActual = 0;
        UuidVentaActual = _ventaLocal.Uuid;
        FolioVentaActual = _ventaLocal.Folio;
        MontoPagadoActual = 0m;
        SaldoActual = 0m;

        GuardarBorradorLocal();
        OnPropertyChanged(nameof(EsVentaSinConexion));
    }


    /// <summary>
    /// Se fue el internet con una venta del servidor a medias: los productos
    /// ya confirmados pasan a una venta en la caja y el borrador del servidor
    /// se cancela al sincronizar. Si ya había pagos registrados no se
    /// convierte (para no cobrar dos veces).
    /// </summary>
    /// <summary>
    /// true si la operación debe hacerse en la caja: ya es venta sin
    /// conexión, o no hay internet y la venta del servidor se pudo pasar.
    /// </summary>
    private bool UsarVentaLocal() =>
        EsVentaSinConexion ||
        (!ConexionService.EstaEnLinea && PasarVentaASinConexion());


    private bool PasarVentaASinConexion()
    {
        if (_ventaLocal is not null)
            return true;

        if (IdVentaActual <= 0)
        {
            IniciarVentaLocal();
            return true;
        }

        if (MontoPagadoActual > 0 || _pagoSinConfirmar)
        {
            MensajeError =
                "Se perdió la conexión después de registrar un pago. Espera a que regrese el internet para terminar esta venta.";
            return false;
        }

        var idAbandonada = IdVentaActual;

        // Las líneas que no alcanzó a confirmar el servidor se descartan.
        foreach (var pendiente in Carrito.Where(d => d.IdDetalle <= 0).ToList())
            Carrito.Remove(pendiente);

        VentasPendientesService.RegistrarBorradorAbandonado(idAbandonada);
        IniciarVentaLocal();

        foreach (var detalle in Carrito)
        {
            detalle.IdDetalle = _siguienteIdDetalleLocal++;
            detalle.EstaSincronizando = false;
            detalle.TieneErrorSincronizacion = false;
            detalle.TiposDisponibles = new List<TipoPrecioDisponible>();
            ActualizarLimiteLocal(detalle);
        }

        GuardarBorradorLocal();
        ActualizarTotales();
        return true;
    }


    private void DescartarVentaLocal()
    {
        if (_ventaLocal is not null)
            VentasPendientesService.BorrarBorrador(_ventaLocal.IdCaja);

        _ventaLocal = null;

        IdVentaActual = 0;
        UuidVentaActual = string.Empty;
        FolioVentaActual = string.Empty;
        MontoPagadoActual = 0m;
        SaldoActual = 0m;

        Carrito.Clear();
        ActualizarTotales();
        OnPropertyChanged(nameof(EsVentaSinConexion));
    }


    /// <summary>Guarda el carrito en disco (se recupera si se va la luz).</summary>
    private void GuardarBorradorLocal()
    {
        if (_ventaLocal is null)
            return;

        _ventaLocal.Lineas = Carrito
            .Select(d => new LineaVentaOffline
            {
                IdProducto = d.Producto.Id,
                IdPresentacion = d.Producto.IdPresentacion,
                Nombre = d.Producto.Nombre,
                NombrePresentacion = d.Producto.NombrePresentacion,
                UnidadMedida = d.UnidadMedida,
                CodigoBarras = d.Producto.CodigoBarras,
                Categoria = d.Producto.Categoria,
                Cantidad = d.Cantidad,
                FactorConversion = d.Producto.FactorConversion,
                PrecioUnitario = d.PrecioUnitario,
                TipoPrecio = string.IsNullOrWhiteSpace(d.TipoPrecioAplicado) ? "MENUDEO" : d.TipoPrecioAplicado!
            })
            .ToList();

        try
        {
            VentasPendientesService.GuardarBorrador(_ventaLocal);
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo guardar la venta en la caja: {ex.Message}";
        }
    }


    /// <summary>
    /// Al abrir Ventas: si quedó una venta sin conexión a medias (apagón,
    /// cierre del programa), se recupera tal cual.
    /// </summary>
    private bool RecuperarVentaLocal()
    {
        if (PosSession.IdCaja <= 0)
            return false;

        var borrador = VentasPendientesService.ObtenerBorrador(PosSession.IdCaja);

        if (borrador is null)
            return false;

        // Ya se había cobrado y encolado justo antes del apagón.
        if (VentasPendientesService.EstaEncolada(borrador.Uuid) || borrador.Lineas.Count == 0)
        {
            VentasPendientesService.BorrarBorrador(PosSession.IdCaja);
            return false;
        }

        // Borrador de otro turno: se conserva el turno con el que se creó.
        _ventaLocal = borrador;

        VentaFinalizadaCorrectamente = false;

        IdVentaActual = 0;
        UuidVentaActual = borrador.Uuid;
        FolioVentaActual = borrador.Folio;
        // Pagos que alcanzaron a registrarse antes del apagón (pago mixto).
        MontoPagadoActual = borrador.Pagado;
        SaldoActual = Math.Max(0m, borrador.Total - borrador.Pagado);

        Carrito.Clear();

        foreach (var linea in borrador.Lineas)
        {
            var producto = new Producto
            {
                Id = linea.IdProducto,
                IdPresentacion = linea.IdPresentacion,
                CodigoBarras = linea.CodigoBarras,
                Nombre = linea.Nombre,
                NombrePresentacion = linea.NombrePresentacion,
                Categoria = linea.Categoria,
                Precio = linea.PrecioUnitario,
                FactorConversion = linea.FactorConversion,
                UnidadMedida = linea.UnidadMedida,
                Inicial = string.IsNullOrWhiteSpace(linea.Nombre) ? "?" : linea.Nombre[..1].ToUpperInvariant(),
                FondoHex = "#EFF6FF",
                ColorHex = "#2563EB",
                ImagenBitmap = _productosCatalogoCompleto
                    .FirstOrDefault(p => p.Id == linea.IdProducto)?.ImagenBitmap
            };

            var detalle = new DetalleVenta(producto)
            {
                IdDetalle = _siguienteIdDetalleLocal++,
                UnidadMedida = linea.UnidadMedida,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                PrecioAutomatico = true,
                TipoPrecioAplicado = linea.TipoPrecio
            };

            ConectarDetalle(detalle);
            ActualizarLimiteLocal(detalle);
            Carrito.Add(detalle);
        }

        ActualizarTotales();
        OnPropertyChanged(nameof(EsVentaSinConexion));
        return true;
    }


    private void ConectarDetalle(DetalleVenta detalle)
    {
        detalle.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(DetalleVenta.Cantidad)
                or nameof(DetalleVenta.Importe)
                or nameof(DetalleVenta.PrecioUnitario))
            {
                ActualizarTotales();
            }
        };
    }


    // =========================================================
    // REGRESÓ EL INTERNET CON UNA VENTA SIN CONEXIÓN A MEDIAS
    // =========================================================

    /// <summary>La ventana de cobro está abierta (no se toca la venta).</summary>
    public bool CobroEnCurso { get; set; }

    private bool _pasandoVentaAlServidor;

    /// <summary>Venta local que el servidor ya rechazó pasar (no se reintenta).</summary>
    private string _uuidNoPasable = string.Empty;


    /// <summary>
    /// Si hay internet y el carrito es una venta sin conexión que aún no se
    /// cobra, se pasa al servidor para que vuelvan todas las formas de pago.
    /// Solo cuando ya no quedan ventas por enviar (para no chocar folios).
    /// </summary>
    public void IntentarPasarVentaAlServidor()
    {
        var local = _ventaLocal;

        if (local is null ||
            _pasandoVentaAlServidor ||
            CobroEnCurso ||
            EstaProcesandoVenta ||
            !ConexionService.EstaEnLinea ||
            !PosSession.TieneTurnoActivo ||
            local.Estado != VentaOffline.EstadoBorrador ||
            local.IdTurno != PosSession.IdTurno ||
            local.Lineas.Count == 0 ||
            local.Pagos.Count > 0 ||
            local.Uuid == _uuidNoPasable)
            return;

        try
        {
            if (VentasPendientesService.ContarPorEnviar(idCaja: PosSession.IdCaja) > 0)
                return;
        }
        catch
        {
            return;
        }

        _ = PasarVentaAlServidorAsync(local);
    }


    private async Task PasarVentaAlServidorAsync(VentaOffline local)
    {
        _pasandoVentaAlServidor = true;
        EstaProcesandoVenta = true;

        var idServidor = 0;
        var pasada = false;

        try
        {
            GuardarBorradorLocal();

            var creada = await _ventasService.CrearVentaAsync(new CrearVentaRequest
            {
                IdEmpresa = PosSession.IdEmpresa,
                IdUnidadOperativa = PosSession.IdUnidadOperativa,
                IdCaja = PosSession.IdCaja,
                IdTurno = PosSession.IdTurno,
                IdCliente = ClientePosSeleccionado?.Id,
                TipoCliente = ClientePosSeleccionado is null ? "PUBLICO_GENERAL" : "FRECUENTE",
                TipoVenta = "CONTADO",
                RequiereFactura = false,
                Folio = $"POS-{DateTime.Now:yyyyMMddHHmmssfff}",
                Observaciones = string.Empty
            });

            // Sin respuesta del servidor: se queda en la caja y se reintenta después.
            if (creada.Res != 1 || creada.Data is null)
                return;

            idServidor = creada.Data.Id;

            foreach (var linea in local.Lineas)
            {
                var agregado = await _ventasService.AgregarProductoAsync(
                    idServidor,
                    new AgregarProductoVentaRequest
                    {
                        IdProducto = linea.IdProducto,
                        IdPresentacion = linea.IdPresentacion,
                        CantidadComercial = linea.Cantidad,
                        FactorConversion = linea.FactorConversion,
                        PrecioUnitario = linea.PrecioUnitario,
                        DescuentoPorcentaje = 0,
                        DescripcionSnapshot = $"{linea.Nombre} - {linea.NombrePresentacion}"
                    });

                if (agregado.Res == 1 && agregado.Data is not null)
                    continue;

                // El servidor rechazó un producto (por ejemplo, sin existencia):
                // la venta sigue en la caja, solo efectivo, y no se reintenta.
                if (ConexionService.EstaEnLinea)
                {
                    _uuidNoPasable = local.Uuid;
                    MensajeError =
                        $"Esta venta sigue sin conexión (efectivo o tarjeta manual): {linea.Nombre} — " +
                        (string.IsNullOrWhiteSpace(agregado.Msg) ? "el servidor no aceptó el producto." : agregado.Msg);
                }

                return;
            }

            pasada = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo pasar la venta sin conexión al servidor: {ex.Message}");
        }
        finally
        {
            // Venta a medias en el servidor: se cancela sola al sincronizar.
            if (!pasada && idServidor > 0)
                VentasPendientesService.RegistrarBorradorAbandonado(idServidor);

            if (pasada)
            {
                _ventaLocal = null;
                VentasPendientesService.BorrarBorrador(local.IdCaja);
                IdVentaActual = idServidor;
                OnPropertyChanged(nameof(EsVentaSinConexion));
            }

            EstaProcesandoVenta = false;
            _pasandoVentaAlServidor = false;
        }

        // Se carga tal como quedó en el servidor (precios, tipos de precio, etc.).
        await CargarVentaActualAsync();
    }


    // =========================================================
    // PRODUCTOS
    // =========================================================

    /// <summary>Agrega al carrito una línea nueva de la venta sin conexión.</summary>
    private bool AgregarLineaLocal(DetalleVenta nuevoDetalle)
    {
        if (BloquearPorPagosLocales())
        {
            Carrito.Remove(nuevoDetalle);
            ActualizarTotales();
            return false;
        }

        var precio = PrecioMenudeoLocal(
            nuevoDetalle.Producto.Id,
            nuevoDetalle.Producto.IdPresentacion);

        if (precio is null)
        {
            Carrito.Remove(nuevoDetalle);
            ActualizarTotales();
            MensajeError =
                $"{nuevoDetalle.Producto.Nombre} no tiene precio de menudeo vigente en la copia del catálogo.";
            return false;
        }

        if (!ValidarExistenciaLocal(nuevoDetalle, nuevoDetalle.Cantidad, out var mensaje))
        {
            Carrito.Remove(nuevoDetalle);
            ActualizarTotales();
            MensajeError = mensaje;
            return false;
        }

        nuevoDetalle.IdDetalle = _siguienteIdDetalleLocal++;
        nuevoDetalle.Producto.Precio = precio.Value;
        nuevoDetalle.PrecioUnitario = precio.Value;
        nuevoDetalle.PrecioAutomatico = true;
        nuevoDetalle.TipoPrecioAplicado = "MENUDEO";
        nuevoDetalle.TiposDisponibles = new List<TipoPrecioDisponible>();
        nuevoDetalle.TieneErrorSincronizacion = false;
        nuevoDetalle.EstaSincronizando = false;

        ActualizarLimiteLocal(nuevoDetalle);
        GuardarBorradorLocal();
        ActualizarTotales();
        MensajeError = string.Empty;
        return true;
    }


    private bool CambiarCantidadLocal(DetalleVenta detalle, decimal cantidadNueva)
    {
        if (BloquearPorPagosLocales())
            return false;

        if (cantidadNueva <= 0)
        {
            MensajeError = "La cantidad debe ser mayor a cero.";
            return false;
        }

        if (!ValidarExistenciaLocal(detalle, cantidadNueva, out var mensaje))
        {
            MensajeError = mensaje;
            return false;
        }

        detalle.Cantidad = cantidadNueva;
        ActualizarLimiteLocal(detalle);
        GuardarBorradorLocal();
        ActualizarTotales();
        MensajeError = string.Empty;
        return true;
    }


    private void EliminarLineaLocal(DetalleVenta detalle)
    {
        if (BloquearPorPagosLocales())
            return;

        Carrito.Remove(detalle);

        if (Carrito.Count == 0)
        {
            DescartarVentaLocal();
            return;
        }

        GuardarBorradorLocal();
        ActualizarTotales();
    }


    // =========================================================
    // PRECIO LOCAL
    // =========================================================

    /// <summary>
    /// Precio MENUDEO vigente hoy según la copia del catálogo; es el mismo
    /// precio que pone el servidor a una línea nueva.
    /// </summary>
    private decimal? PrecioMenudeoLocal(int idProducto, int idPresentacion)
    {
        var presentacion = _productosCatalogoCompleto
            .FirstOrDefault(p => p.Id == idProducto)?
            .Presentaciones
            .FirstOrDefault(p => p.Id == idPresentacion);

        if (presentacion is null)
            return null;

        var hoy = DateTime.Today;

        var vigente = presentacion.Precios
            .Where(p => string.Equals(p.TipoPrecio?.Trim(), "MENUDEO", StringComparison.OrdinalIgnoreCase))
            .Where(p => p.Estatus != 3 && p.Precio > 0)
            .Where(p => !DateTime.TryParse(p.FechaInicio, out var inicio) || inicio.Date <= hoy)
            .Where(p => string.IsNullOrWhiteSpace(p.FechaFin) ||
                        !DateTime.TryParse(p.FechaFin, out var fin) || fin.Date >= hoy)
            .OrderByDescending(p => DateTime.TryParse(p.FechaInicio, out var inicio) ? inicio : DateTime.MinValue)
            .ThenByDescending(p => p.Id)
            .FirstOrDefault();

        return vigente?.Precio;
    }


    // =========================================================
    // INVENTARIO LOCAL
    // =========================================================

    /// <summary>
    /// Existencia que la caja cree que hay: la del catálogo descargado
    /// menos lo vendido sin conexión que el servidor aún no descuenta.
    /// null = el producto no tiene inventario configurado.
    /// </summary>
    private decimal? ExistenciaLocal(int idProducto, int idPresentacion)
    {
        var presentacion = _productosCatalogoCompleto
            .FirstOrDefault(p => p.Id == idProducto)?
            .Presentaciones
            .FirstOrDefault(p => p.Id == idPresentacion);

        if (presentacion is null || !presentacion.TieneInventarioConfigurado)
            return null;

        decimal vendidoSinDescontar = 0m;

        try
        {
            foreach (var venta in VentasPendientesService.ObtenerDelTurno(PosSession.IdTurno))
            {
                var yaIncluida =
                    venta.Estado == VentaOffline.EstadoEnviada &&
                    venta.FechaEnvio is { } enviada &&
                    enviada <= _fechaCatalogo;

                if (yaIncluida || venta.Estado == VentaOffline.EstadoRechazada || venta.Cancelacion is not null)
                    continue;

                vendidoSinDescontar += venta.Lineas
                    .Where(l => l.IdProducto == idProducto && l.IdPresentacion == idPresentacion)
                    .Sum(l => l.Cantidad);
            }
        }
        catch
        {
            // Si no se puede leer la cola, se usa la existencia del catálogo.
        }

        return presentacion.ExistenciaDisponible - vendidoSinDescontar;
    }


    private bool ValidarExistenciaLocal(DetalleVenta detalle, decimal cantidad, out string mensaje)
    {
        mensaje = string.Empty;

        var existencia = ExistenciaLocal(detalle.Producto.Id, detalle.Producto.IdPresentacion);

        if (existencia is null)
        {
            mensaje = $"{detalle.Producto.Nombre} no tiene inventario configurado en este punto de venta.";
            return false;
        }

        // Lo que ya hay del mismo producto en otras líneas del carrito.
        var enOtrasLineas = Carrito
            .Where(d => !ReferenceEquals(d, detalle) &&
                        d.Producto.Id == detalle.Producto.Id &&
                        d.Producto.IdPresentacion == detalle.Producto.IdPresentacion)
            .Sum(d => d.Cantidad);

        var disponible = existencia.Value - enOtrasLineas;

        if (cantidad > disponible)
        {
            mensaje = disponible <= 0
                ? $"Sin existencia de {detalle.Producto.Nombre} según la última copia del inventario."
                : $"Solo hay {disponible:0.###} de {detalle.Producto.Nombre} según la última copia del inventario.";
            return false;
        }

        return true;
    }


    private void ActualizarLimiteLocal(DetalleVenta detalle)
    {
        var existencia = ExistenciaLocal(detalle.Producto.Id, detalle.Producto.IdPresentacion);
        if (existencia is null)
            return;

        var enOtrasLineas = Carrito
            .Where(d => !ReferenceEquals(d, detalle) &&
                        d.Producto.Id == detalle.Producto.Id &&
                        d.Producto.IdPresentacion == detalle.Producto.IdPresentacion)
            .Sum(d => d.Cantidad);

        var disponible = Math.Max(0m, existencia.Value - enOtrasLineas);

        detalle.ExistenciaDisponible = Math.Max(0m, disponible - detalle.Cantidad);
        detalle.MaximoLinea = disponible;
    }


    // =========================================================
    // COBRO Y TICKET
    // =========================================================

    private Task<bool> CobrarEfectivoLocalAsync(decimal importeAplicado, decimal recibido, decimal cambio) =>
        RegistrarPagoLocalAsync(
            new PagoVentaOffline { IdFormaPago = FormasPagoPos.Efectivo, Importe = importeAplicado },
            recibido,
            cambio);


    /// <summary>
    /// Tarjeta cobrada en la terminal bancaria sin internet: el cajero captura
    /// la autorización del voucher y el pago se envía con la venta.
    /// </summary>
    public Task<bool> RegistrarPagoTarjetaLocalAsync(
        decimal importe, bool esDebito, string autorizacion, string ultimosDigitos) =>
        RegistrarPagoTarjetaLocalAsync(
            esDebito ? FormasPagoPos.TarjetaDebito : FormasPagoPos.TarjetaCredito,
            importe,
            autorizacion,
            ReferenciaTarjetaExterna(ultimosDigitos, sinConexion: true),
            alRegistrar: null);


    private async Task<bool> RegistrarPagoTarjetaLocalAsync(
        int idFormaPago, decimal importe, string autorizacion, string referencia, Action? alRegistrar)
    {
        if (EstaProcesandoVenta)
            return false;

        // Venta del servidor y se fue el internet: pasa a la caja.
        if (!EsVentaSinConexion && !PasarVentaASinConexion())
            return false;

        EstaProcesandoVenta = true;

        try
        {
            var pago = new PagoVentaOffline
            {
                IdFormaPago = idFormaPago,
                Importe = importe,
                Autorizacion = (autorizacion ?? string.Empty).Trim(),
                Referencia = referencia
            };

            var ok = await RegistrarPagoLocalAsync(pago, 0m, 0m);

            // El pago quedó guardado en la caja (aunque luego falle terminar la venta).
            if (ok || _ventaLocal?.Pagos.Contains(pago) == true)
                alRegistrar?.Invoke();

            return ok;
        }
        finally
        {
            EstaProcesandoVenta = false;
        }
    }


    /// <summary>Ya hay pagos en la venta sin conexión: el carrito no se puede cambiar.</summary>
    private bool BloquearPorPagosLocales()
    {
        if (_ventaLocal is null || _ventaLocal.Pagos.Count == 0)
            return false;

        MensajeError =
            "Esta venta ya tiene un pago registrado: termina de cobrarla antes de cambiar los productos.";
        return true;
    }


    private async Task<bool> RegistrarPagoLocalAsync(PagoVentaOffline pago, decimal recibido, decimal cambio)
    {
        if (_ventaLocal is null || Carrito.Count == 0)
        {
            MensajeError = "No hay productos para cobrar.";
            return false;
        }

        GuardarBorradorLocal();

        var total = _ventaLocal.Total;
        var saldo = Math.Round(total - _ventaLocal.Pagado, 2);

        pago.Importe = Math.Round(pago.Importe, 2);

        if (pago.Importe <= 0m)
        {
            MensajeError = "El importe del pago debe ser mayor a cero.";
            return false;
        }

        if (pago.Importe > saldo + 0.005m)
        {
            MensajeError = $"El pago (${pago.Importe:N2}) es mayor al saldo pendiente (${saldo:N2}).";
            return false;
        }

        // Se guarda el pago antes de seguir: si se va la luz no se pierde.
        _ventaLocal.Pagos.Add(pago);

        if (pago.IdFormaPago == FormasPagoPos.Efectivo)
        {
            _ventaLocal.Recibido += recibido;
            _ventaLocal.Cambio = cambio;
        }

        try
        {
            VentasPendientesService.GuardarBorrador(_ventaLocal);
        }
        catch (Exception ex)
        {
            _ventaLocal.Pagos.Remove(pago);
            MensajeError = $"No se pudo guardar el pago en la caja: {ex.Message}";
            return false;
        }

        MontoPagadoActual = _ventaLocal.Pagado;
        SaldoActual = Math.Max(0m, total - _ventaLocal.Pagado);

        if (pago.IdFormaPago == FormasPagoPos.Efectivo)
        {
            UltimoMontoRecibido = recibido;
            UltimoCambio = cambio;
        }

        // Pago mixto: falta saldo, la ventana de cobro sigue abierta.
        if (total - _ventaLocal.Pagado > 0.005m)
            return true;

        return await TerminarVentaLocalAsync();
    }


    /// <summary>
    /// Venta a crédito sin conexión: se valida con la copia de crédito de la
    /// caja (ya descuenta lo vendido sin conexión) y se registra en cartera
    /// cuando el servidor la reciba.
    /// </summary>
    private async Task<bool> FinalizarVentaCreditoLocalAsync()
    {
        if (ClientePosSeleccionado is null)
        {
            MensajeError = "Selecciona un cliente para realizar la venta a crédito.";
            return false;
        }

        if (EstaProcesandoVenta)
            return false;

        if (!EsVentaSinConexion && !PasarVentaASinConexion())
            return false;

        if (_ventaLocal is null || Carrito.Count == 0)
        {
            MensajeError = "No hay productos para vender a crédito.";
            return false;
        }

        // Igual que en línea: a crédito no se permiten pagos previos.
        if (_ventaLocal.Pagos.Count > 0)
        {
            MensajeError = "Esta venta ya tiene un pago registrado; no se puede pasar a crédito.";
            return false;
        }

        GuardarBorradorLocal();

        // Sin internet se vuelve a revisar con la copia (con internet, la
        // ventana de crédito ya lo consultó con el servidor).
        if (!ConexionService.EstaEnLinea)
        {
            var estado = NovaCoreESDM.Services.Credito.CreditoService
                .EstadoSinConexion(ClientePosSeleccionado.Id, _ventaLocal.Total);

            if (estado.Data is not { PuedeVenderCredito: true })
            {
                MensajeError = estado.Data is null
                    ? estado.Msg
                    : string.Join(" ", estado.Data.MotivosBloqueo);
                return false;
            }
        }

        EstaProcesandoVenta = true;

        try
        {
            _ventaLocal.TipoVenta = "CREDITO";
            _ventaLocal.IdCliente = ClientePosSeleccionado.Id;
            _ventaLocal.NombreCliente = ClientePosSeleccionado.Nombre;

            return await TerminarVentaLocalAsync();
        }
        finally
        {
            EstaProcesandoVenta = false;
        }
    }


    private async Task<bool> TerminarVentaLocalAsync()
    {
        if (_ventaLocal is null)
            return false;

        var total = _ventaLocal.Total;
        _ventaLocal.FechaCobro = DateTime.Now;

        // Cliente de la venta (también de contado, para facturar o consultar).
        if (ClientePosSeleccionado is not null && _ventaLocal.IdCliente is null)
        {
            _ventaLocal.IdCliente = ClientePosSeleccionado.Id;
            _ventaLocal.NombreCliente = ClientePosSeleccionado.Nombre;
        }

        // Folio con el mismo formato que los demás tickets (UO-SERIE-00000000).
        if (string.IsNullOrWhiteSpace(_ventaLocal.FolioTicket) &&
            ConsecutivoTicketLocal.Reservar(_ventaLocal.IdCaja) is { } consecutivo)
        {
            _ventaLocal.FolioTicket = consecutivo.Folio;
            _ventaLocal.SerieTicket = consecutivo.Serie;
            _ventaLocal.NumeroTicket = consecutivo.Numero;
        }

        // 1. Se guarda primero: si falla la impresión o el cajón, la venta no se pierde.
        try
        {
            VentasPendientesService.Encolar(_ventaLocal);
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo guardar la venta en la caja: {ex.Message}";
            return false;
        }

        var venta = _ventaLocal;
        _ventaLocal = null;
        OnPropertyChanged(nameof(EsVentaSinConexion));

        var efectivo = venta.Pagos
            .Where(p => p.IdFormaPago == FormasPagoPos.Efectivo)
            .Sum(p => p.Importe);

        var tarjeta = venta.Pagos
            .Where(p => p.IdFormaPago is FormasPagoPos.TarjetaCredito or FormasPagoPos.TarjetaDebito)
            .Sum(p => p.Importe);

        // 2. Cajón (solo si entró efectivo).
        if (efectivo > 0)
        {
            try
            {
                await _printerService.AbrirCajonAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Venta sin conexión guardada, pero falló el cajón: {ex.Message}");
            }
        }

        MontoPagadoActual = venta.EsCredito ? 0m : total;
        SaldoActual = 0m;
        var folioImpreso =
            string.IsNullOrWhiteSpace(venta.FolioTicket) ? venta.Folio : venta.FolioTicket;

        UltimoFolioTicket = folioImpreso;
        VentaFinalizadaCorrectamente = true;

        // 3. Ticket.
        var configuracion = await _terminalConfigurationService.ObtenerConfiguracionAsync();

        var ticket = new TicketVenta
        {
            Empresa = "GRUPO LUISFER",
            Sistema = "ESDM",
            UnidadOperativa = configuracion?.UnidadCodigo ?? string.Empty,
            FolioTicket = folioImpreso,
            Uuid = venta.Uuid,
            Fecha = venta.FechaCobro ?? DateTime.Now,
            Caja = string.IsNullOrWhiteSpace(venta.CodigoCaja) ? PosSession.CodigoCaja : venta.CodigoCaja,
            Cajero = venta.NombreCajero,
            Productos = venta.Lineas.Select(l => new TicketVentaDetalle
            {
                Producto = l.Nombre,
                Presentacion = l.NombrePresentacion,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                Importe = l.Importe
            }).ToList(),
            Subtotal = total,
            Total = total,
            Efectivo = efectivo,
            Tarjeta = tarjeta,
            Recibido = efectivo > 0 ? venta.Recibido : 0m,
            Cambio = efectivo > 0 ? venta.Cambio : 0m,
            Pagado = venta.EsCredito ? 0m : total,
            FormaPago = venta.EsCredito
                ? "Crédito"
                : efectivo > 0 && tarjeta > 0
                    ? "PAGO MIXTO"
                    : tarjeta > 0 ? "Tarjeta" : "Efectivo",
            ContenidoQr = "https://plataformaluisfer.com/Plataforma_LuisFer/auth/login"
        };

        await EntregarTicketYLimpiarAsync(ticket);

        ActualizarEstadoConexion();
        return true;
    }


    /// <summary>
    /// Con internet: toma de la caja su serie y último folio, para que sin
    /// conexión los tickets sigan la misma numeración.
    /// </summary>
    private static async Task AprenderConsecutivoDelServidorAsync()
    {
        if (!ConexionService.EstaEnLinea || PosSession.IdCaja <= 0)
            return;

        try
        {
            var cajas = await new NovaCoreESDM.Services.Cajas.CajasService()
                .ObtenerCajasAsync(PosSession.IdUnidadOperativa);

            var caja = cajas.Data?.FirstOrDefault(c => c.Id == PosSession.IdCaja);

            if (caja is not null)
                ConsecutivoTicketLocal.Aprender(caja.Id, caja.UnidadCodigo, caja.SerieTicket, caja.UltimoFolio);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo leer el consecutivo de la caja: {ex.Message}");
        }
    }


    private static string GenerarFolioSinConexion()
    {
        var caja = new string((PosSession.CodigoCaja ?? string.Empty)
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .Take(10)
            .ToArray());

        if (caja.Length == 0)
            caja = $"C{PosSession.IdCaja}";

        var sufijo = Convert.ToHexString(RandomNumberGenerator.GetBytes(2));

        return $"SC-{caja}-{DateTime.Now:yyMMdd-HHmmss}-{sufijo}";
    }


    /// <summary>
    /// Mensaje para las funciones que necesitan internet. Devuelve true si
    /// la acción debe detenerse.
    /// </summary>
    private bool BloquearSinConexion(string accion)
    {
        if (!EsVentaSinConexion && ConexionService.EstaEnLinea)
            return false;

        MensajeError = $"Sin conexión no se puede {accion}. {MensajeSoloEfectivo}";
        return true;
    }
}
