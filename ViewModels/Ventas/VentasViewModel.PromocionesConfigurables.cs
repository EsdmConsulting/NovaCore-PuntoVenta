using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Ventas;
using NovaCoreESDM.Services.Offline;

namespace NovaCoreESDM.ViewModels.Ventas;

/// <summary>
/// CHG-0005 fase 2: promociones dadas de alta en el ERP (compra X lleva Y,
/// X por Y, % en la pieza N, precio por cantidad). Al cambiar el carrito se
/// le pregunta al servidor cuáles cumple la venta; el cajero las aplica con
/// la autorización del supervisor y el servidor ajusta las piezas.
/// Sin internet no se ofrecen.
/// </summary>
public partial class VentasViewModel
{
    /// <summary>Promociones que la venta cumple ahora (para el aviso).</summary>
    public ObservableCollection<PromocionVenta> PromocionesDisponibles { get; } = new();

    public bool HayPromocionesDisponibles => PromocionesDisponibles.Count > 0;

    /// <summary>La atiende VentasView: autorización del supervisor y aplicar.</summary>
    public event Action<PromocionVenta>? SolicitarAplicarPromocion;

    private CancellationTokenSource? _revisionPromociones;


    /// <summary>Se llama en cada cambio del carrito: revisa 0.6 s después del último.</summary>
    private void ProgramarRevisionPromociones()
    {
        _revisionPromociones?.Cancel();

        if (IdVentaActual <= 0 || EsVentaSinConexion || !ConexionService.EstaEnLinea || Carrito.Count == 0)
        {
            if (PromocionesDisponibles.Count > 0)
            {
                PromocionesDisponibles.Clear();
                OnPropertyChanged(nameof(HayPromocionesDisponibles));
            }
            return;
        }

        var cts = new CancellationTokenSource();
        _revisionPromociones = cts;
        _ = RevisarPromocionesAsync(cts.Token);
    }


    private async Task RevisarPromocionesAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(600, token);

            var idVenta = IdVentaActual;
            var r = await _ventasService.ObtenerPromocionesVentaAsync(idVenta);

            if (token.IsCancellationRequested || idVenta != IdVentaActual || r.Res != 1 || r.Data is null)
                return;

            // Solo las que se pueden aplicar ahora (las ya aplicadas no estorban).
            var disponibles = r.Data.Promociones
                .Where(p => p.PuedeAplicar && p.Veces > 0)
                .ToList();

            Dispatcher.UIThread.Post(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                PromocionesDisponibles.Clear();
                foreach (var p in disponibles)
                    PromocionesDisponibles.Add(p);

                OnPropertyChanged(nameof(HayPromocionesDisponibles));
            });
        }
        catch (OperationCanceledException)
        {
            // Hubo otro cambio: se revisa después.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudieron revisar las promociones: {ex.Message}");
        }
    }


    [RelayCommand]
    private void AplicarPromocion(PromocionVenta? promocion)
    {
        if (promocion is null)
            return;

        if (BloquearSinConexion("aplicar promociones"))
            return;

        if (MontoPagadoActual > 0)
        {
            MensajeError = "La venta ya tiene un pago registrado: termina de cobrarla antes de cambiar los productos.";
            return;
        }

        SolicitarAplicarPromocion?.Invoke(promocion);
    }


    /// <summary>Aplica la promoción en el servidor (ya autorizada) y vuelve a leer la venta.</summary>
    public async Task<bool> AplicarPromocionAutorizadaAsync(PromocionVenta promocion)
    {
        if (IdVentaActual <= 0)
            return false;

        var r = await _ventasService.AplicarPromocionAsync(IdVentaActual, promocion.IdPromocion);

        if (r.Res != 1)
        {
            MensajeError = string.IsNullOrWhiteSpace(r.Msg) ? "No fue posible aplicar la promoción." : r.Msg;
            ProgramarRevisionPromociones();
            return false;
        }

        // El servidor separó las piezas: se vuelve a leer la venta, conservando
        // lo quitado con autorización (CHG-0002) para el ticket.
        var cancelados = _productosCancelados.ToList();

        await CargarVentaActualAsync();

        _productosCancelados.Clear();
        _productosCancelados.AddRange(cancelados);

        MensajeError = string.Empty;
        ProgramarRevisionPromociones();

        Console.WriteLine(
            $"Promoción aplicada: {r.Data?.Nombre ?? promocion.Nombre} ×{r.Data?.Veces ?? promocion.Veces}, ahorro ${r.Data?.Ahorro ?? promocion.Ahorro:N2}.");
        return true;
    }
}
