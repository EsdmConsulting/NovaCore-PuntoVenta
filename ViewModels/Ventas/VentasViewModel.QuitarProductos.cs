using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using NovaCoreESDM.Models;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Tickets;
using NovaCoreESDM.Services.Cancelaciones;
using NovaCoreESDM.Services.Offline;

namespace NovaCoreESDM.ViewModels.Ventas;

/// <summary>
/// CHG-0002: quitar productos de la venta.
///
/// - Deshacer lo último que se agregó (el último producto escaneado, o las
///   piezas que se acaban de sumar a una línea) es libre.
/// - Quitar o bajar la cantidad de cualquier producto anterior pide la
///   autorización de un administrador, solo para ese producto.
/// - Lo quitado con autorización sale en el ticket del cliente y el servidor
///   guarda quién lo autorizó (sin internet se envía con la venta).
///
/// "Último producto" = el que se agregó más recientemente, sin importar las
/// cancelaciones previas: después de deshacerlo ya no hay otro libre.
/// </summary>
public partial class VentasViewModel
{
    /// <summary>Resultado de pedir permiso para quitar.</summary>
    public sealed class PermisoQuitarProducto
    {
        public static readonly PermisoQuitarProducto Libre = new();

        public bool RequirioAutorizacion { get; init; }

        /// <summary>Se autorizó con internet: el servidor lo valida con la autorización abierta.</summary>
        public bool AutorizadoEnServidor { get; init; }

        /// <summary>Se pidió con la ventana de administrador (hay que cerrar esa autorización).</summary>
        public bool UsoVentana { get; init; }

        public int IdUsuarioAutoriza { get; init; }
        public string UsuarioAutoriza { get; init; } = string.Empty;
        public string NombreAutoriza { get; init; } = string.Empty;
    }

    /// <summary>Administrador que autorizó (lo devuelve la ventana de autorización).</summary>
    public sealed class AdministradorAutorizo
    {
        public int IdUsuario { get; init; }
        public string Usuario { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
    }


    /// <summary>
    /// La asigna VentasView: abre la ventana de autorización de administrador
    /// (título, descripción). null = canceló o no se autorizó.
    /// </summary>
    public Func<string, string, Task<AdministradorAutorizo?>>? SolicitarAutorizacionQuitarProducto { get; set; }


    private DetalleVenta? _ultimoAgregado;
    private decimal _cantidadAntesUltimoAgregado;
    private readonly List<ProductoCanceladoVenta> _productosCancelados = new();
    private bool _seguimientoCarritoIniciado;

    /// <summary>Productos quitados con autorización en la venta actual (para el ticket).</summary>
    public IReadOnlyList<ProductoCanceladoVenta> ProductosCanceladosVenta => _productosCancelados;


    private void IniciarSeguimientoCarrito()
    {
        if (_seguimientoCarritoIniciado)
            return;

        _seguimientoCarritoIniciado = true;

        // Carrito vacío = terminó o se descartó la venta: nada pasa a la siguiente.
        Carrito.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset || Carrito.Count == 0)
            {
                _ultimoAgregado = null;
                _cantidadAntesUltimoAgregado = 0m;
                _productosCancelados.Clear();
            }
        };
    }


    /// <summary>Se acaba de agregar un producto (línea nueva o piezas sumadas).</summary>
    private void RegistrarAgregado(DetalleVenta detalle, decimal cantidadAntes)
    {
        IniciarSeguimientoCarrito();

        _ultimoAgregado = detalle;
        _cantidadAntesUltimoAgregado = Math.Max(0m, cantidadAntes);
    }


    /// <summary>El producto que se puede quitar con F12 (lo último agregado).</summary>
    public DetalleVenta? UltimoProductoAgregado =>
        _ultimoAgregado is not null && Carrito.Contains(_ultimoAgregado)
            ? _ultimoAgregado
            : null;


    /// <summary>¿Bajar la línea a <paramref name="cantidadNueva"/> solo deshace lo último agregado?</summary>
    private bool EsDeshacerUltimoAgregado(DetalleVenta detalle, decimal cantidadNueva) =>
        ReferenceEquals(detalle, _ultimoAgregado) &&
        cantidadNueva >= _cantidadAntesUltimoAgregado;


    /// <summary>
    /// Permiso para bajar la línea a <paramref name="cantidadNueva"/> (0 = quitarla).
    /// null = no se autorizó: la venta queda igual.
    /// </summary>
    private async Task<PermisoQuitarProducto?> PedirPermisoQuitarAsync(DetalleVenta detalle, decimal cantidadNueva)
    {
        IniciarSeguimientoCarrito();

        if (cantidadNueva >= detalle.Cantidad || EsDeshacerUltimoAgregado(detalle, cantidadNueva))
            return PermisoQuitarProducto.Libre;

        // El administrador que está cobrando no se pide permiso a sí mismo.
        if (PosSession.EsAdministrador)
        {
            return new PermisoQuitarProducto
            {
                RequirioAutorizacion = true,
                IdUsuarioAutoriza = PosSession.IdUsuario,
                UsuarioAutoriza = PosSession.Usuario ?? string.Empty,
                NombreAutoriza = PosSession.NombreUsuario ?? string.Empty
            };
        }

        if (SolicitarAutorizacionQuitarProducto is null)
        {
            MensajeError = "Para quitar un producto anterior se necesita la autorización de un administrador.";
            return null;
        }

        var nombre = detalle.Producto?.Nombre ?? "el producto";
        var accion = cantidadNueva <= 0m
            ? $"Quitar «{nombre}» de la venta"
            : $"Bajar «{nombre}» de {FormatoCantidad(detalle.Cantidad)} a {FormatoCantidad(cantidadNueva)}";

        var admin = await SolicitarAutorizacionQuitarProducto(
            "Autorizar cancelación de producto",
            $"{accion}. No es el último producto escaneado, por eso necesita la autorización de un administrador. " +
            "La autorización es solo para este producto y aparecerá en el ticket.");

        if (admin is null)
        {
            MensajeError = "No se autorizó: el producto sigue en la venta.";
            return null;
        }

        return new PermisoQuitarProducto
        {
            RequirioAutorizacion = true,
            AutorizadoEnServidor = ConexionService.EstaEnLinea,
            UsoVentana = true,
            IdUsuarioAutoriza = admin.IdUsuario,
            UsuarioAutoriza = admin.Usuario,
            NombreAutoriza = admin.Nombre
        };
    }


    /// <summary>La autorización sirve para un solo producto: se cierra al terminar.</summary>
    private static async Task CerrarPermisoAsync(PermisoQuitarProducto permiso)
    {
        if (!permiso.UsoVentana)
            return;

        try
        {
            await new CancelacionesService().CerrarAutorizacionAsync();
        }
        catch
        {
            // El servidor la vence solo.
        }
    }


    /// <summary>Se quitó <paramref name="cantidad"/> de la línea (ya confirmado).</summary>
    private void RegistrarQuitado(DetalleVenta detalle, decimal cantidad, PermisoQuitarProducto permiso)
    {
        if (cantidad <= 0m)
            return;

        // Lo último agregado ya se deshizo (aunque haya sido con permiso).
        if (ReferenceEquals(detalle, _ultimoAgregado) &&
            (!Carrito.Contains(detalle) || detalle.Cantidad <= _cantidadAntesUltimoAgregado))
        {
            _ultimoAgregado = null;
            _cantidadAntesUltimoAgregado = 0m;
        }

        // Sin productos ya no hay venta: no hay ticket que lo muestre.
        if (Carrito.Count == 0)
            return;

        var precio = detalle.PrecioUnitario;

        var cancelado = new ProductoCanceladoVenta
        {
            IdProducto = detalle.Producto?.Id ?? 0,
            IdPresentacion = detalle.Producto?.IdPresentacion ?? 0,
            Producto = detalle.Producto?.Nombre ?? string.Empty,
            Presentacion = detalle.Producto?.NombrePresentacion ?? string.Empty,
            Cantidad = cantidad,
            PrecioUnitario = precio,
            Importe = Math.Round(cantidad * precio, 2),
            Tipo = Carrito.Contains(detalle)
                ? ProductoCanceladoVenta.TipoCantidadReducida
                : ProductoCanceladoVenta.TipoLineaEliminada,
            RequirioAutorizacion = permiso.RequirioAutorizacion,
            IdUsuarioAutoriza = permiso.IdUsuarioAutoriza,
            UsuarioAutoriza = permiso.UsuarioAutoriza,
            NombreAutoriza = permiso.NombreAutoriza,
            Fecha = DateTime.Now
        };

        if (permiso.RequirioAutorizacion)
        {
            _productosCancelados.Add(cancelado);

            Console.WriteLine(
                $"Producto cancelado con autorización de {permiso.UsuarioAutoriza}: " +
                $"{cancelado.Producto} x{FormatoCantidad(cantidad)} (${cancelado.Importe:N2}).");
        }

        // Sin conexión el servidor se entera al recibir la venta.
        if (UsarVentaLocal() && _ventaLocal is not null)
        {
            _ventaLocal.ProductosCancelados.Add(cancelado);
            GuardarBorradorLocal();
        }
    }


    /// <summary>Al recuperar una venta sin conexión, vuelven sus cancelaciones para el ticket.</summary>
    private void RestaurarProductosCancelados(VentaOffline venta)
    {
        IniciarSeguimientoCarrito();

        _productosCancelados.Clear();
        _productosCancelados.AddRange(venta.ProductosCancelados.Where(p => p.RequirioAutorizacion));
    }


    private List<TicketProductoCancelado> ProductosCanceladosParaTicket() =>
        _productosCancelados
            .Select(p => new TicketProductoCancelado
            {
                Producto = p.Producto,
                Presentacion = p.Presentacion,
                Cantidad = p.Cantidad,
                PrecioUnitario = p.PrecioUnitario,
                Importe = p.Importe,
                Autorizo = string.IsNullOrWhiteSpace(p.NombreAutoriza) ? p.UsuarioAutoriza : p.NombreAutoriza
            })
            .ToList();


    private static string FormatoCantidad(decimal cantidad) =>
        cantidad.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);


    /// <summary>F12: deshace lo último agregado (o quita la última línea, con permiso si hace falta).</summary>
    public async Task QuitarUltimoAgregadoAsync()
    {
        var detalle = UltimoProductoAgregado ?? Carrito.LastOrDefault();

        if (detalle is null)
            return;

        if (detalle.EstaSincronizando)
        {
            MensajeError = "El último producto aún se está registrando, espera un momento.";
            return;
        }

        // Se sumaron piezas a una línea que ya estaba: solo se regresan esas.
        if (ReferenceEquals(detalle, _ultimoAgregado) && _cantidadAntesUltimoAgregado > 0m)
        {
            await ActualizarCantidadManualAsync(detalle, _cantidadAntesUltimoAgregado);
            return;
        }

        await EliminarProductoAsync(detalle);
    }
}
