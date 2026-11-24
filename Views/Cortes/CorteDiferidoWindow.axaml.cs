using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Ventas;
using NovaCoreESDM.Services.Offline;
using NovaCoreESDM.Services.Printing;

namespace NovaCoreESDM.Views.Cortes;

/// <summary>
/// Corte sin internet: el cajero captura el efectivo contado, se imprime un
/// pre-corte provisional y el corte oficial se cierra en el servidor al
/// volver la conexión (después de enviar las ventas y abonos del turno).
/// </summary>
public partial class CorteDiferidoWindow : Window
{
    private readonly int _idTurno;
    private readonly TicketVentaService.DatosPreCorte _datos;
    private bool _procesando;

    public CorteDiferido? Corte { get; private set; }


    // Constructor sin parámetros para el diseñador.
    public CorteDiferidoWindow() : this(0) { }


    public CorteDiferidoWindow(int idTurno)
    {
        InitializeComponent();

        _idTurno = idTurno;
        _datos = Resumir(idTurno);

        TxtInformacionTurno.Text =
            $"Turno {idTurno} · {_datos.Caja} · {_datos.Cajero}";

        TxtVentas.Text = _datos.VentasSinConexion.ToString();
        TxtEfectivo.Text = $"${_datos.Efectivo:N2}";
        TxtTarjeta.Text = $"${_datos.Tarjeta:N2}";
        TxtCredito.Text = $"${_datos.Credito:N2}";
        TxtAbonos.Text = $"{_datos.Abonos} · ${_datos.AbonosTotal:N2}";
        TxtCanceladas.Text = _datos.VentasCanceladas.ToString();

        Opened += (_, _) => TxtEfectivoContado.Focus();
    }


    private static TicketVentaService.DatosPreCorte Resumir(int idTurno)
    {
        var datos = new TicketVentaService.DatosPreCorte
        {
            IdTurno = idTurno,
            Caja = PosSession.CodigoCaja ?? string.Empty,
            Cajero = PosSession.NombreUsuario ?? string.Empty
        };

        if (idTurno <= 0)
            return datos;

        try
        {
            var ventas = VentasPendientesService.ObtenerDelTurno(idTurno)
                .Where(v => v.Estado != VentaOffline.EstadoRechazada)
                .ToList();

            var vigentes = ventas.Where(v => v.Cancelacion is null).ToList();

            datos.VentasSinConexion = vigentes.Count;
            datos.VentasCanceladas = ventas.Count - vigentes.Count;
            datos.Efectivo = vigentes.SelectMany(v => v.Pagos)
                .Where(p => p.IdFormaPago == FormasPagoPos.Efectivo).Sum(p => p.Importe);
            datos.Tarjeta = vigentes.SelectMany(v => v.Pagos)
                .Where(p => p.IdFormaPago is FormasPagoPos.TarjetaCredito or FormasPagoPos.TarjetaDebito)
                .Sum(p => p.Importe);
            datos.Credito = vigentes.Where(v => v.EsCredito).Sum(v => v.Total);

            var abonos = AbonosPendientesService.ObtenerDelTurno(idTurno);
            datos.Abonos = abonos.Count;
            datos.AbonosTotal = abonos.Sum(a => a.Total);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo resumir el turno sin conexión: {ex.Message}");
        }

        return datos;
    }


    private async void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        if (_procesando)
            return;

        TextoMensaje.Text = string.Empty;

        var texto = (TxtEfectivoContado.Text ?? string.Empty)
            .Replace("$", string.Empty)
            .Replace(",", string.Empty)
            .Trim();

        if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var efectivo) ||
            efectivo < 0m)
        {
            TextoMensaje.Text = "Ingresa el efectivo contado físicamente.";
            TxtEfectivoContado.Focus();
            return;
        }

        if (_idTurno <= 0)
        {
            TextoMensaje.Text = "No se encontró un turno válido.";
            return;
        }

        // Igual que en línea: no se corta con una venta a medias en el carrito.
        if (VentasPendientesService.ObtenerBorrador(PosSession.IdCaja) is { Lineas.Count: > 0 })
        {
            TextoMensaje.Text =
                "Hay una venta pendiente en el carrito. Cóbrala o cancélala antes de hacer el corte.";
            return;
        }

        if (CorteDiferidoService.TieneCortePendiente(_idTurno))
        {
            TextoMensaje.Text = "Este turno ya tiene un corte sin conexión registrado.";
            return;
        }

        _procesando = true;
        BtnConfirmar.IsEnabled = false;
        BtnCancelar.IsEnabled = false;

        try
        {
            var corte = new CorteDiferido
            {
                IdTurno = _idTurno,
                IdCaja = PosSession.IdCaja,
                Cajero = _datos.Cajero,
                EfectivoDeclarado = Math.Round(efectivo, 2),
                Observaciones = (TxtObservaciones.Text ?? string.Empty).Trim(),
                Fecha = DateTime.Now
            };

            // 1. Se guarda primero: si falla la impresora, el corte no se pierde.
            CorteDiferidoService.Registrar(corte);
            Corte = corte;

            // 2. Pre-corte provisional.
            _datos.Fecha = corte.Fecha;
            _datos.EfectivoContado = corte.EfectivoDeclarado;
            _datos.Observaciones = corte.Observaciones;

            try
            {
                await new TicketVentaService().ImprimirPreCorteAsync(_datos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Corte sin conexión guardado, pero no se pudo imprimir el pre-corte: {ex.Message}");
            }

            Close(true);
        }
        catch (Exception ex)
        {
            TextoMensaje.Text = $"No se pudo guardar el corte en la caja: {ex.Message}";
            BtnConfirmar.IsEnabled = true;
            BtnCancelar.IsEnabled = true;
        }
        finally
        {
            _procesando = false;
        }
    }


    private void Cancelar_Click(object? sender, RoutedEventArgs e) =>
        Close(false);
}
