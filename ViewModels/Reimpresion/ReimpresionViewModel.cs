using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Cancelaciones;
using NovaCoreESDM.Services.Printing;
using NovaCoreESDM.Services.Reimpresion;

namespace NovaCoreESDM.ViewModels.Reimpresion;

public partial class ReimpresionViewModel : ViewModelBase
{
    private readonly ReimpresionService _servicio = new();


    // =========================================================
    // NAVEGACIÓN (la asigna MainViewModel)
    // =========================================================

    public Action? Salir { get; set; }


    // =========================================================
    // ADMINISTRADOR QUE AUTORIZÓ
    // =========================================================

    [ObservableProperty]
    private string administradorAutorizado = string.Empty;


    // =========================================================
    // FILTROS
    // =========================================================

    [ObservableProperty]
    private DateTimeOffset? fechaDesde = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? fechaHasta = DateTimeOffset.Now;

    [ObservableProperty]
    private string folioBusqueda = string.Empty;


    // =========================================================
    // LISTADO
    // =========================================================

    public ObservableCollection<VentaCancelacionItem> Ventas { get; } = new();

    [ObservableProperty]
    private bool sinResultados;


    // =========================================================
    // ESTADO
    // =========================================================

    [ObservableProperty]
    private bool estaCargando;

    [ObservableProperty]
    private bool estaImprimiendo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayError))]
    private string mensajeError = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayExito))]
    private string mensajeExito = string.Empty;

    public bool HayError =>
        !string.IsNullOrWhiteSpace(MensajeError);

    public bool HayExito =>
        !string.IsNullOrWhiteSpace(MensajeExito);


    // =========================================================
    // ENTRAR AL MÓDULO (ya validado un administrador)
    // =========================================================

    public async Task IniciarAsync(string nombreAdministrador)
    {
        AdministradorAutorizado = nombreAdministrador;

        FechaDesde = DateTimeOffset.Now;
        FechaHasta = DateTimeOffset.Now;
        FolioBusqueda = string.Empty;

        await BuscarInternoAsync();
    }


    // =========================================================
    // SALIR DEL MÓDULO: se cierra el acceso de administrador
    // =========================================================

    public async Task CerrarAutorizacionAsync()
    {
        try
        {
            await _servicio.CerrarAutorizacionAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error cerrando autorización de reimpresión: {ex.Message}");
        }
        finally
        {
            AdministradorAutorizado = string.Empty;
            Ventas.Clear();
            SinResultados = false;
            LimpiarMensajes();
        }
    }


    [RelayCommand]
    private void CerrarAcceso()
    {
        Salir?.Invoke();
    }


    // =========================================================
    // BUSCAR
    // =========================================================

    [RelayCommand]
    private async Task BuscarAsync()
    {
        await BuscarInternoAsync();
    }


    private async Task BuscarInternoAsync()
    {
        if (string.IsNullOrWhiteSpace(AdministradorAutorizado))
        {
            MensajeError = "Se requiere autorización de un administrador.";
            return;
        }

        LimpiarMensajes();

        try
        {
            EstaCargando = true;
            Ventas.Clear();
            SinResultados = false;

            var desde = FechaDesde?.DateTime ?? DateTime.Today;
            var hasta = FechaHasta?.DateTime ?? desde;

            var resultado =
                await _servicio.BuscarAsync(
                    desde,
                    hasta,
                    FolioBusqueda);

            if (!resultado.Exito)
            {
                MensajeError = resultado.Mensaje;
                return;
            }

            foreach (var venta in resultado.Ventas)
                Ventas.Add(venta);

            SinResultados = Ventas.Count == 0;
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible consultar las ventas: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
        }
    }


    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        FechaDesde = DateTimeOffset.Now;
        FechaHasta = DateTimeOffset.Now;
        FolioBusqueda = string.Empty;

        await BuscarInternoAsync();
    }


    // =========================================================
    // REIMPRIMIR
    // =========================================================

    [RelayCommand]
    private async Task ReimprimirAsync(VentaCancelacionItem? venta)
    {
        if (venta is null || EstaImprimiendo)
            return;

        LimpiarMensajes();

        if (!venta.PuedeReimprimir)
        {
            MensajeError =
                "Solo se pueden reimprimir ventas finalizadas.";
            return;
        }

        try
        {
            EstaImprimiendo = true;

            MensajeExito =
                await _servicio.ReimprimirAsync(
                    venta.Id,
                    AdministradorAutorizado);
        }
        catch (ImpresoraNoConfiguradaException ex)
        {
            MensajeError = ex.Message;
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible reimprimir: {ex.Message}";
        }
        finally
        {
            EstaImprimiendo = false;
        }
    }


    private void LimpiarMensajes()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }
}
