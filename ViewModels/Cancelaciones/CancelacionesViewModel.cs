using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Cancelaciones;
using NovaCoreESDM.Services.Cancelaciones;
using NovaCoreESDM.ViewModels;

namespace NovaCoreESDM.ViewModels.Cancelaciones;

public partial class CancelacionesViewModel : ViewModelBase
{
    // =========================================================
    // SERVICIOS
    // =========================================================

    private readonly CancelacionesService _cancelacionesService =
        new();


    // =========================================================
    // AUTORIZACIÓN ADMINISTRATIVA
    // =========================================================

    [ObservableProperty]
    private string usuarioAdministrador =
        string.Empty;


    [ObservableProperty]
    private string passwordAdministrador =
        string.Empty;


    [ObservableProperty]
    private bool administradorAutorizado;


    [ObservableProperty]
    private string nombreAdministrador =
        string.Empty;


    // =========================================================
    // FILTROS
    // =========================================================

    [ObservableProperty]
    private DateTimeOffset? fechaSeleccionada =
        DateTimeOffset.Now;


    [ObservableProperty]
    private string folioBusqueda =
        string.Empty;


    [ObservableProperty]
    private string estadoSeleccionado =
        string.Empty;


    // =========================================================
    // LISTADO
    // =========================================================

    public ObservableCollection<VentaCancelacionItem> Ventas
    {
        get;
    } = new();


    [ObservableProperty]
    private VentaCancelacionItem? ventaSeleccionada;


    // =========================================================
    // DETALLE
    // =========================================================

    [ObservableProperty]
    private VentaCancelacionDetalle? ventaDetalle;


    public ObservableCollection<DetalleProductoCancelacion>
        Detalles
    {
        get;
    } = new();


    [ObservableProperty]
    private bool puedeCancelarVenta;


    [ObservableProperty]
    private string motivoCancelacion =
        string.Empty;


    // =========================================================
    // ESTADO DE PANTALLA
    // =========================================================

    [ObservableProperty]
    private bool estaCargando;


    [ObservableProperty]
    private bool estaAutorizando;


    [ObservableProperty]
    private bool estaCancelando;


    [ObservableProperty]
    private string mensajeError =
        string.Empty;


    [ObservableProperty]
    private string mensajeExito =
        string.Empty;


    // =========================================================
    // PROPIEDADES AUXILIARES PARA LA UI
    // =========================================================

    public bool HayError =>
        !string.IsNullOrWhiteSpace(
            MensajeError);


    public bool HayMensajeExito =>
        !string.IsNullOrWhiteSpace(
            MensajeExito);


    public bool HayVentaSeleccionada =>
        VentaDetalle is not null;


    public bool PuedeEjecutarCancelacion =>
        VentaDetalle is not null &&
        PuedeCancelarVenta &&
        !EstaCancelando;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public CancelacionesViewModel()
    {
        EstadoSeleccionado =
            string.Empty;
    }

    
    public async Task CerrarAutorizacionAsync()
    {
        try
        {
            await _cancelacionesService
                .CerrarAutorizacionAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error cerrando autorización de cancelaciones: {ex.Message}"
            );
        }
        finally
        {
            LimpiarAutorizacionLocal();
        }
    }
    
    
    private void LimpiarAutorizacionLocal()
    {
        AdministradorAutorizado =
            false;

        NombreAdministrador =
            string.Empty;

        UsuarioAdministrador =
            string.Empty;

        PasswordAdministrador =
            string.Empty;

        VentaSeleccionada =
            null;

        VentaDetalle =
            null;

        MotivoCancelacion =
            string.Empty;

        PuedeCancelarVenta =
            false;

        Ventas.Clear();
        Detalles.Clear();

        MensajeError =
            string.Empty;
    }

    // =========================================================
    // AUTORIZAR ADMINISTRADOR
    // =========================================================

    [RelayCommand]
    private async Task AutorizarAdministradorAsync()
    {
        LimpiarMensajes();


        if (string.IsNullOrWhiteSpace(
                UsuarioAdministrador))
        {
            MensajeError =
                "Ingresa el usuario del administrador.";

            return;
        }


        if (string.IsNullOrWhiteSpace(
                PasswordAdministrador))
        {
            MensajeError =
                "Ingresa la contraseña del administrador.";

            return;
        }


        try
        {
            EstaAutorizando =
                true;


            var respuesta =
                await _cancelacionesService
                    .AutorizarAdministradorAsync(
                        UsuarioAdministrador,
                        PasswordAdministrador
                    );


            if (respuesta.Res != 1)
            {
                MensajeError =
                    respuesta.Msg ??
                    "No fue posible autorizar al administrador.";

                return;
            }


            AdministradorAutorizado =
                true;


            NombreAdministrador =
                respuesta.Data?.Nombre ??
                respuesta.Data?.Usuario ??
                UsuarioAdministrador;


            // Ya no necesitamos conservar el password
            // en memoria después de autorizar.
            PasswordAdministrador =
                string.Empty;


            MensajeExito =
                $"Administrador {NombreAdministrador} autorizado.";


            // =================================================
            // CARGAR AUTOMÁTICAMENTE LAS VENTAS DE HOY
            // =================================================

            await BuscarVentasInternoAsync();
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible autorizar al administrador: {ex.Message}";
        }
        finally
        {
            EstaAutorizando =
                false;
        }
    }


    // =========================================================
    // BUSCAR VENTAS
    // =========================================================

    [RelayCommand]
    private async Task BuscarVentasAsync()
    {
        LimpiarMensajes();


        if (!AdministradorAutorizado)
        {
            MensajeError =
                "Primero debes autorizar a un administrador.";

            return;
        }


        await BuscarVentasInternoAsync();
    }


    private async Task BuscarVentasInternoAsync()
    {
        try
        {
            EstaCargando =
                true;


            Ventas.Clear();


            LimpiarDetalle();


            var fecha =
                FechaSeleccionada?.DateTime
                ?? DateTime.Today;


            var respuesta =
                await _cancelacionesService
                    .ObtenerVentasAsync(
                        fecha,
                        string.IsNullOrWhiteSpace(
                            FolioBusqueda)
                            ? null
                            : FolioBusqueda.Trim(),
                        string.IsNullOrWhiteSpace(
                            EstadoSeleccionado)
                            ? null
                            : EstadoSeleccionado.Trim()
                    );


            if (respuesta.Res != 1)
            {
                MensajeError =
                    respuesta.Msg ??
                    "No fue posible consultar las ventas.";

                return;
            }


            if (respuesta.Data?.Ventas is not null)
            {
                foreach (
                    var venta
                    in respuesta.Data.Ventas)
                {
                    Ventas.Add(
                        venta);
                }
            }


            if (Ventas.Count == 0)
            {
                MensajeExito =
                    "No se encontraron ventas con los filtros seleccionados.";
            }
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible consultar las ventas: {ex.Message}";
        }
        finally
        {
            EstaCargando =
                false;
        }
    }


    // =========================================================
    // VER DETALLE
    // =========================================================

    [RelayCommand]
    private async Task VerDetalleAsync(
        VentaCancelacionItem? venta)
    {
        LimpiarMensajes();


        if (!AdministradorAutorizado)
        {
            MensajeError =
                "La operación requiere autorización administrativa.";

            return;
        }


        if (venta is null)
        {
            return;
        }


        if (venta.Id <= 0)
        {
            MensajeError =
                "La venta seleccionada no es válida.";

            return;
        }


        try
        {
            EstaCargando =
                true;


            VentaSeleccionada =
                venta;


            LimpiarDetalle(
                conservarSeleccion: true);


            var respuesta =
                await _cancelacionesService
                    .ObtenerDetalleAsync(
                        venta.Id
                    );


            if (respuesta.Res != 1 ||
                respuesta.Data is null)
            {
                MensajeError =
                    respuesta.Msg ??
                    "No fue posible consultar el detalle de la venta.";

                return;
            }


            VentaDetalle =
                respuesta.Data.Venta;


            PuedeCancelarVenta =
                respuesta.Data.PuedeCancelar;


            Detalles.Clear();


            if (respuesta.Data.Detalles is not null)
            {
                foreach (
                    var detalle
                    in respuesta.Data.Detalles)
                {
                    Detalles.Add(
                        detalle);
                }
            }


            MotivoCancelacion =
                string.Empty;


            ActualizarPropiedadesCalculadas();
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible consultar el detalle de la venta: {ex.Message}";
        }
        finally
        {
            EstaCargando =
                false;
        }
    }


    // =========================================================
    // CANCELAR VENTA
    // =========================================================
    //
    // IMPORTANTE:
    //
    // Este comando todavía NO muestra un cuadro de confirmación.
    //
    // La ventana que haremos después será la responsable de
    // preguntar:
    //
    // "¿Realmente deseas cancelar esta venta?"
    //
    // y únicamente entonces llamará este comando.
    // =========================================================

    [RelayCommand]
    private async Task CancelarVentaAsync()
    {
        LimpiarMensajes();


        if (!AdministradorAutorizado)
        {
            MensajeError =
                "La operación requiere autorización administrativa.";

            return;
        }


        if (VentaDetalle is null)
        {
            MensajeError =
                "Selecciona una venta.";

            return;
        }


        if (!PuedeCancelarVenta)
        {
            MensajeError =
                "Esta venta no puede ser cancelada.";

            return;
        }


        if (string.IsNullOrWhiteSpace(
                MotivoCancelacion))
        {
            MensajeError =
                "Debes indicar el motivo de la cancelación.";

            return;
        }


        var motivo =
            MotivoCancelacion.Trim();


        if (motivo.Length > 500)
        {
            MensajeError =
                "El motivo no puede superar los 500 caracteres.";

            return;
        }


        try
        {
            EstaCancelando =
                true;


            var idVenta =
                VentaDetalle.Id;


            var respuesta =
                await _cancelacionesService
                    .CancelarVentaAsync(
                        idVenta,
                        motivo
                    );


            if (respuesta.Res != 1)
            {
                MensajeError =
                    respuesta.Msg ??
                    "No fue posible cancelar la venta.";

                return;
            }


            MensajeExito =
                respuesta.Msg ??
                "Venta cancelada correctamente.";


            // =================================================
            // ACTUALIZAR DETALLE LOCAL
            // =================================================

            PuedeCancelarVenta =
                false;


            if (VentaDetalle is not null)
            {
                VentaDetalle.Estado =
                    "CANCELADA";


                VentaDetalle.MotivoCancelacion =
                    motivo;


                VentaDetalle.UsuarioCancelacion =
                    respuesta.Data?
                        .Administrador?
                        .Usuario;


                VentaDetalle.FechaCancelacion =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
            }


            // =================================================
            // ACTUALIZAR FILA DEL LISTADO
            // =================================================

            if (VentaSeleccionada is not null)
            {
                VentaSeleccionada.Estado =
                    "CANCELADA";


                VentaSeleccionada.MotivoCancelacion =
                    motivo;


                VentaSeleccionada.UsuarioCancelacion =
                    respuesta.Data?
                        .Administrador?
                        .Usuario;


                VentaSeleccionada.FechaCancelacion =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }


            MotivoCancelacion =
                string.Empty;


            ActualizarPropiedadesCalculadas();
            
            // =================================================
            // CERRAR AUTORIZACIÓN DESPUÉS DE CANCELAR
            // =================================================

            await CerrarAutorizacionAsync();


            // =================================================
            // REFRESCAR DESDE EL SERVIDOR
            // =================================================
            //
            // Así no dependemos únicamente de los cambios
            // realizados en memoria.
            // =================================================
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible cancelar la venta: {ex.Message}";
        }
        finally
        {
            EstaCancelando =
                false;

            ActualizarPropiedadesCalculadas();
        }
    }


    // =========================================================
    // LIMPIAR FILTROS
    // =========================================================

    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        FolioBusqueda =
            string.Empty;


        EstadoSeleccionado =
            string.Empty;


        FechaSeleccionada =
            DateTimeOffset.Now;


        LimpiarMensajes();


        if (AdministradorAutorizado)
        {
            await BuscarVentasInternoAsync();
        }
    }


    // =========================================================
    // CERRAR DETALLE
    // =========================================================

    [RelayCommand]
    private void CerrarDetalle()
    {
        LimpiarDetalle();
        LimpiarMensajes();
    }


    // =========================================================
    // LIMPIAR DETALLE
    // =========================================================

    private void LimpiarDetalle(
        bool conservarSeleccion = false)
    {
        VentaDetalle =
            null;


        Detalles.Clear();


        PuedeCancelarVenta =
            false;


        MotivoCancelacion =
            string.Empty;


        if (!conservarSeleccion)
        {
            VentaSeleccionada =
                null;
        }


        ActualizarPropiedadesCalculadas();
    }


    // =========================================================
    // LIMPIAR MENSAJES
    // =========================================================

    private void LimpiarMensajes()
    {
        MensajeError =
            string.Empty;


        MensajeExito =
            string.Empty;


        ActualizarPropiedadesCalculadas();
    }


    // =========================================================
    // NOTIFICAR PROPIEDADES CALCULADAS
    // =========================================================

    private void ActualizarPropiedadesCalculadas()
    {
        OnPropertyChanged(
            nameof(HayError));


        OnPropertyChanged(
            nameof(HayMensajeExito));


        OnPropertyChanged(
            nameof(HayVentaSeleccionada));


        OnPropertyChanged(
            nameof(PuedeEjecutarCancelacion));
    }


    // =========================================================
    // CAMBIOS DE PROPIEDADES
    // =========================================================

    partial void OnMensajeErrorChanged(
        string value)
    {
        OnPropertyChanged(
            nameof(HayError));
    }


    partial void OnMensajeExitoChanged(
        string value)
    {
        OnPropertyChanged(
            nameof(HayMensajeExito));
    }


    partial void OnVentaDetalleChanged(
        VentaCancelacionDetalle? value)
    {
        OnPropertyChanged(
            nameof(HayVentaSeleccionada));

        OnPropertyChanged(
            nameof(PuedeEjecutarCancelacion));
    }


    partial void OnPuedeCancelarVentaChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(PuedeEjecutarCancelacion));
    }


    partial void OnEstaCancelandoChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(PuedeEjecutarCancelacion));
    }
}