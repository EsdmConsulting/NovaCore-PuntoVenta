using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Turno;
using NovaCoreESDM.Services.Turnos;

namespace NovaCoreESDM.ViewModels.Inicio;

public partial class InicioViewModel
    : NovaCoreESDM.ViewModels.ViewModelBase
{
    private readonly TurnosService
        _turnosService;


    // =========================================================
    // ESTADO
    // =========================================================

    [ObservableProperty]
    private bool _estaCargando;


    [ObservableProperty]
    private string _mensajeError =
        string.Empty;


    // =========================================================
    // INDICADORES
    // =========================================================

    [ObservableProperty]
    private decimal _ventasTurno;


    [ObservableProperty]
    private int _tickets;


    [ObservableProperty]
    private decimal _efectivo;


    [ObservableProperty]
    private decimal _tarjeta;


    // =========================================================
    // INFORMACIÓN DEL TURNO
    // =========================================================

    [ObservableProperty]
    private int _idTurno;


    [ObservableProperty]
    private string _estadoTurno =
        string.Empty;


    [ObservableProperty]
    private string _caja =
        string.Empty;


    [ObservableProperty]
    private string _usuarioApertura =
        string.Empty;


    [ObservableProperty]
    private decimal _fondoInicial;


    [ObservableProperty]
    private decimal _efectivoEsperado;


    [ObservableProperty]
    private bool _puedeCerrarTurno;


    // =========================================================
    // ACTIVIDAD DEL TURNO
    // =========================================================

    public ObservableCollection<ActividadTurnoItem> ActividadTurno { get; }
        = new();


    public bool TieneActividad =>
        ActividadTurno.Count > 0;


    public bool NoTieneActividad =>
        !TieneActividad;


    // =========================================================
    // NAVEGACIÓN DESDE INICIO
    // =========================================================

    public Action? IrANuevaVenta { get; set; }

    public Action? IrACortes { get; set; }


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public InicioViewModel()
    {
        _turnosService =
            new TurnosService();
    }


    // =========================================================
    // NAVEGACIÓN
    // =========================================================

    [RelayCommand]
    private void NuevaVenta()
    {
        IrANuevaVenta?.Invoke();
    }


    [RelayCommand]
    private void ConsultarCortes()
    {
        IrACortes?.Invoke();
    }


    // =========================================================
    // CARGAR DASHBOARD
    // =========================================================

    [RelayCommand]
    public async Task CargarAsync()
    {
        if (EstaCargando)
        {
            return;
        }


        MensajeError =
            string.Empty;


        // =====================================================
        // VALIDAR TURNO ACTIVO
        // =====================================================

        if (PosSession.IdTurno <= 0)
        {
            MensajeError =
                "No existe un turno activo.";

            LimpiarDatos();

            return;
        }


        EstaCargando =
            true;


        try
        {
            var idTurnoActual =
                PosSession.IdTurno;


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "CARGANDO DASHBOARD DEL TURNO:");

            Console.WriteLine(
                $"IdTurno: {idTurnoActual}");

            Console.WriteLine(
                "====================================");


            // =================================================
            // RESUMEN DEL TURNO
            // =================================================

            var resultado =
                await _turnosService
                    .ObtenerResumenTurnoAsync(
                        idTurnoActual);


            // =================================================
            // VALIDAR RESPUESTA
            // =================================================

            if (resultado.Res != 1)
            {
                MensajeError =
                    string.IsNullOrWhiteSpace(
                        resultado.Msg)

                        ? "No fue posible cargar el resumen del turno."

                        : resultado.Msg;


                LimpiarDatos();

                return;
            }


            if (resultado.Data is null)
            {
                MensajeError =
                    "El servidor no devolvió el resumen del turno.";

                LimpiarDatos();

                return;
            }


            var resumen =
                resultado.Data;


            // =================================================
            // INDICADORES PRINCIPALES
            // =================================================

            VentasTurno =
                resumen.Ventas?.Total ??
                0m;


            Tickets =
                resumen.Ventas?.Tickets ??
                0;


            Efectivo =
                resumen.Cobros?.Efectivo ??
                0m;


            Tarjeta =
                resumen.Cobros?.Tarjetas ??
                0m;


            // =================================================
            // INFORMACIÓN DEL TURNO
            // =================================================

            IdTurno =
                resumen.Turno?.Id ??
                idTurnoActual;


            EstadoTurno =
                resumen.Turno?.Estado ??
                string.Empty;


            Caja =
                resumen.Turno?.CajaNombre ??
                PosSession.NombreCaja ??
                string.Empty;


            UsuarioApertura =
                ObtenerUsuarioApertura(
                    resumen.Turno?.UsuarioAperturaNombre,
                    resumen.Turno?.UsuarioApertura);


            FondoInicial =
                resumen.Turno?.FondoInicial ??
                0m;


            PuedeCerrarTurno =
                resumen.Turno?.PuedeCerrar ??
                false;


            // =================================================
            // EFECTIVO ESPERADO
            // =================================================

            EfectivoEsperado =
                resumen.Caja?.EfectivoEsperado ??
                0m;


            // =================================================
            // ACTIVIDAD DEL TURNO
            // =================================================

            await CargarActividadAsync(
                idTurnoActual);


            // =================================================
            // DEBUG
            // =================================================

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "DASHBOARD CARGADO:");

            Console.WriteLine(
                $"Ventas: {VentasTurno:C2}");

            Console.WriteLine(
                $"Tickets: {Tickets}");

            Console.WriteLine(
                $"Efectivo: {Efectivo:C2}");

            Console.WriteLine(
                $"Tarjeta: {Tarjeta:C2}");

            Console.WriteLine(
                $"Fondo inicial: {FondoInicial:C2}");

            Console.WriteLine(
                $"Efectivo esperado: {EfectivoEsperado:C2}");

            Console.WriteLine(
                $"Puede cerrar: {PuedeCerrarTurno}");

            Console.WriteLine(
                $"Actividad: {ActividadTurno.Count} movimientos");

            Console.WriteLine(
                "====================================");
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible cargar el dashboard: {ex.Message}";


            LimpiarDatos();


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR CARGANDO DASHBOARD:");

            Console.WriteLine(
                ex);

            Console.WriteLine(
                "====================================");
        }
        finally
        {
            EstaCargando =
                false;
        }
    }


    // =========================================================
    // CARGAR ACTIVIDAD DEL TURNO
    // =========================================================

    private async Task CargarActividadAsync(
        int idTurno)
    {
        ActividadTurno.Clear();

        NotificarEstadoActividad();


        try
        {
            var resultado =
                await _turnosService
                    .ObtenerActividadTurnoAsync(
                        idTurno,
                        10);


            if (resultado.Res != 1)
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "NO FUE POSIBLE CARGAR ACTIVIDAD:");

                Console.WriteLine(
                    resultado.Msg ??
                    "Sin mensaje del servidor.");

                Console.WriteLine(
                    "====================================");

                return;
            }


            if (resultado.Data?.Actividad is null)
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "EL SERVIDOR NO DEVOLVIÓ ACTIVIDAD.");

                Console.WriteLine(
                    "====================================");

                return;
            }


            foreach (var item in
                     resultado.Data.Actividad)
            {
                ActividadTurno.Add(
                    item);
            }


            NotificarEstadoActividad();


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ACTIVIDAD DEL TURNO CARGADA:");

            Console.WriteLine(
                $"Movimientos: {ActividadTurno.Count}");

            Console.WriteLine(
                "====================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR CARGANDO ACTIVIDAD DEL TURNO:");

            Console.WriteLine(
                ex);

            Console.WriteLine(
                "====================================");
        }
    }


    // =========================================================
    // NOTIFICAR ESTADO DE ACTIVIDAD
    // =========================================================

    private void NotificarEstadoActividad()
    {
        OnPropertyChanged(
            nameof(TieneActividad));

        OnPropertyChanged(
            nameof(NoTieneActividad));
    }


    // =========================================================
    // OBTENER USUARIO APERTURA
    // =========================================================

    private static string ObtenerUsuarioApertura(
        string? nombre,
        string? usuario)
    {
        if (!string.IsNullOrWhiteSpace(
                nombre))
        {
            return nombre;
        }


        if (!string.IsNullOrWhiteSpace(
                usuario))
        {
            return usuario;
        }


        return "—";
    }


    // =========================================================
    // LIMPIAR DATOS
    // =========================================================

    private void LimpiarDatos()
    {
        VentasTurno =
            0m;


        Tickets =
            0;


        Efectivo =
            0m;


        Tarjeta =
            0m;


        IdTurno =
            0;


        EstadoTurno =
            string.Empty;


        Caja =
            string.Empty;


        UsuarioApertura =
            string.Empty;


        FondoInicial =
            0m;


        EfectivoEsperado =
            0m;


        PuedeCerrarTurno =
            false;


        ActividadTurno.Clear();


        NotificarEstadoActividad();
    }
}