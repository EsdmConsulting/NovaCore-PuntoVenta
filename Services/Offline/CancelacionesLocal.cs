using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Cancelaciones;
using NovaCoreESDM.Models.Session;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Cancelaciones sin internet: solo ventas hechas sin conexión que todavía
/// no se envían. Se muestran en la misma pantalla de Cancelaciones con un id
/// local; al cancelarlas se marcan en la cola y se envían ya canceladas.
/// </summary>
public static class CancelacionesLocal
{
    // Ids locales muy altos para no chocar con los del servidor.
    private const int BaseId = 2_000_000_000;
    private static readonly Dictionary<int, string> _uuidPorId = new();
    private static readonly object _candado = new();

    public static bool EsLocal(int idVenta)
    {
        lock (_candado)
            return _uuidPorId.ContainsKey(idVenta);
    }


    private static int IdLocal(string uuid)
    {
        lock (_candado)
        {
            var existente = _uuidPorId.FirstOrDefault(p => p.Value == uuid);
            if (existente.Value is not null)
                return existente.Key;

            var id = BaseId - _uuidPorId.Count;
            _uuidPorId[id] = uuid;
            return id;
        }
    }


    private static VentaOffline? Buscar(int idVenta)
    {
        string? uuid;
        lock (_candado)
            _uuidPorId.TryGetValue(idVenta, out uuid);

        return uuid is null
            ? null
            : VentasPendientesService.ObtenerPendientes().FirstOrDefault(v => v.Uuid == uuid);
    }


    private static string Estado(VentaOffline v) =>
        v.Cancelacion is not null ? "CANCELADA" : "FINALIZADA";

    private static string Folio(VentaOffline v) =>
        string.IsNullOrWhiteSpace(v.FolioTicket) ? v.Folio : v.FolioTicket;


    // =========================================================
    // AUTORIZAR / ADMINISTRADORES
    // =========================================================

    public static AdministradoresListResponse Administradores() =>
        new()
        {
            Res = 1,
            Msg = "Sin conexión: administradores guardados en esta caja.",
            Data = AutorizacionLocal.ObtenerLista()
        };


    public static AutorizarCancelacionResponse Autorizar(string usuario, string password)
    {
        var datos = AutorizacionLocal.Autorizar(usuario, password, out var motivo);

        return datos is null
            ? new AutorizarCancelacionResponse { Res = 0, Msg = motivo }
            : new AutorizarCancelacionResponse
            {
                Res = 1,
                Msg = "Autorizado sin conexión: solo se pueden cancelar ventas hechas sin internet que aún no se envían.",
                Data = datos
            };
    }


    // =========================================================
    // LISTADO Y DETALLE
    // =========================================================

    public static CancelacionesListResponse Ventas(DateTime fecha, string? folio, string? estado)
    {
        var ventas = VentasPendientesService.ObtenerPendientes()
            .Where(v => v.Estado == VentaOffline.EstadoPendiente)
            .Where(v => (v.FechaCobro ?? v.FechaCreacion).Date == fecha.Date)
            .Where(v => string.IsNullOrWhiteSpace(folio) ||
                        Folio(v).Contains(folio.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        v.Folio.Contains(folio.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(v => string.IsNullOrWhiteSpace(estado) ||
                        string.Equals(Estado(v), estado.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v.FechaCobro ?? v.FechaCreacion)
            .Select(v =>
            {
                var fechaVenta = v.FechaCobro ?? v.FechaCreacion;
                return new VentaCancelacionItem
                {
                    Id = IdLocal(v.Uuid),
                    Uuid = v.Uuid,
                    Folio = v.Folio,
                    FolioTicket = v.FolioTicket,
                    IdEmpresa = v.IdEmpresa,
                    IdUnidadOperativa = v.IdUnidadOperativa,
                    IdCaja = v.IdCaja,
                    IdTurno = v.IdTurno,
                    IdCliente = v.IdCliente,
                    TipoVenta = v.TipoVenta,
                    Subtotal = v.Total,
                    Total = v.Total,
                    MontoPagado = v.Pagado,
                    Saldo = v.EsCredito ? v.Total : 0m,
                    Estado = Estado(v),
                    Fecha = fechaVenta.ToString("yyyy-MM-dd"),
                    Hora = fechaVenta.ToString("HH:mm:ss"),
                    IdUsuario = v.IdUsuario,
                    UsuarioRegistro = v.Usuario,
                    UsuarioCancelacion = v.Cancelacion?.UsuarioAutoriza,
                    FechaCancelacion = v.Cancelacion?.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
                    MotivoCancelacion = v.Cancelacion?.Motivo
                };
            })
            .ToList();

        return new CancelacionesListResponse
        {
            Res = 1,
            Msg = "Sin conexión: solo se muestran ventas hechas sin internet que aún no se envían.",
            Data = new CancelacionesListData
            {
                Fecha = fecha.ToString("yyyy-MM-dd"),
                Cantidad = ventas.Count,
                Ventas = ventas
            }
        };
    }


    public static CancelacionDetalleResponse Detalle(int idVenta)
    {
        var v = Buscar(idVenta);

        if (v is null)
        {
            return new CancelacionDetalleResponse
            {
                Res = 0,
                Msg = "La venta ya se envió al servidor; para cancelarla se necesita internet."
            };
        }

        var fechaVenta = v.FechaCobro ?? v.FechaCreacion;
        var linea = 0;

        return new CancelacionDetalleResponse
        {
            Res = 1,
            Data = new CancelacionDetalleData
            {
                Venta = new VentaCancelacionDetalle
                {
                    Id = idVenta,
                    Uuid = v.Uuid,
                    Folio = v.Folio,
                    FolioTicket = v.FolioTicket,
                    IdEmpresa = v.IdEmpresa,
                    IdUnidadOperativa = v.IdUnidadOperativa,
                    IdCaja = v.IdCaja,
                    IdTurno = v.IdTurno,
                    IdCliente = v.IdCliente,
                    TipoVenta = v.TipoVenta,
                    Subtotal = v.Total,
                    Total = v.Total,
                    MontoPagado = v.Pagado,
                    Saldo = v.EsCredito ? v.Total : 0m,
                    Estado = Estado(v),
                    Fecha = fechaVenta.ToString("yyyy-MM-dd"),
                    Hora = fechaVenta.ToString("HH:mm:ss"),
                    IdUsuario = v.IdUsuario,
                    UsuarioRegistro = v.Usuario,
                    UsuarioCancelacion = v.Cancelacion?.UsuarioAutoriza,
                    FechaCancelacion = v.Cancelacion?.Fecha.ToString("yyyy-MM-dd HH:mm:ss"),
                    MotivoCancelacion = v.Cancelacion?.Motivo
                },
                Detalles = v.Lineas.Select(l => new DetalleProductoCancelacion
                {
                    IdDetalle = ++linea,
                    NumeroLinea = linea,
                    IdProducto = l.IdProducto,
                    IdPresentacion = l.IdPresentacion,
                    DescripcionSnapshot = $"{l.Nombre} - {l.NombrePresentacion}",
                    CantidadComercial = l.Cantidad,
                    FactorConversion = l.FactorConversion,
                    CantidadBase = l.Cantidad * l.FactorConversion,
                    PrecioLista = l.PrecioUnitario,
                    PrecioUnitario = l.PrecioUnitario,
                    TipoPrecioAplicado = l.TipoPrecio,
                    Subtotal = l.Importe,
                    TotalLinea = l.Importe,
                    Estado = "ACTIVA",
                    CodigoBarras = l.CodigoBarras,
                    NombreComercial = l.Nombre,
                    NombrePresentacion = l.NombrePresentacion,
                    UnidadMedida = l.UnidadMedida
                }).ToList(),
                PuedeCancelar = v.Cancelacion is null && v.Estado == VentaOffline.EstadoPendiente
            }
        };
    }


    // =========================================================
    // CANCELAR
    // =========================================================

    public static async Task<CancelarVentaResponse> CancelarAsync(int idVenta, string motivo)
    {
        var admin = AutorizacionLocal.AutorizadoSinConexion;

        if (admin is null || admin.IdUsuario <= 0)
        {
            return new CancelarVentaResponse
            {
                Res = 0,
                Msg = "Vuelve a autorizar al administrador para cancelar."
            };
        }

        var venta = Buscar(idVenta);

        if (venta is null)
        {
            return new CancelarVentaResponse
            {
                Res = 0,
                Msg = "La venta ya se envió al servidor; para cancelarla se necesita internet."
            };
        }

        var error = await VentasPendientesService.CancelarPendienteAsync(
            venta.Uuid,
            new CancelacionVentaOffline
            {
                Motivo = motivo,
                IdUsuarioAutoriza = admin.IdUsuario,
                UsuarioAutoriza = admin.Usuario ?? string.Empty,
                NombreAutoriza = admin.Nombre ?? string.Empty,
                Fecha = DateTime.Now
            });

        if (error is not null)
            return new CancelarVentaResponse { Res = 0, Msg = error };

        Console.WriteLine($"Venta sin conexión {venta.Folio} cancelada en la caja por {admin.Usuario} (turno {PosSession.IdTurno}).");

        return new CancelarVentaResponse
        {
            Res = 1,
            Msg = "Venta cancelada en la caja. Se enviará cancelada al servidor (con su inventario de regreso) cuando haya internet."
        };
    }
}
