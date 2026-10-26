using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Avalonia.Threading;

using NovaCoreESDM.Models.Session;

using NovaCoreESDM.ViewModels.Cancelaciones;
using NovaCoreESDM.ViewModels.Configuracion;
using NovaCoreESDM.ViewModels.Cortes;
using NovaCoreESDM.ViewModels.Credito;
using NovaCoreESDM.ViewModels.Inicio;
using NovaCoreESDM.ViewModels.Reimpresion;
using NovaCoreESDM.ViewModels.Ventas;

namespace NovaCoreESDM.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // =========================================================
    // MÓDULOS
    // =========================================================

    public InicioViewModel InicioModulo { get; }

    public VentasViewModel VentasModulo { get; }

    public PagoCreditoViewModel PagoCreditoModulo { get; }

    public CortesViewModel CortesModulo { get; }

    public CancelacionesViewModel CancelacionesModulo { get; }

    public ReimpresionViewModel ReimpresionModulo { get; }

    public ConfiguracionViewModel ConfiguracionModulo { get; }


    // =========================================================
    // AUTORIZACIÓN DE ADMINISTRADOR
    // =========================================================
    //
    // La asigna MainWindow (abre la ventana de usuario/contraseña).
    //
    //   parámetros: título y descripción de la ventana
    //   resultado : nombre del administrador, o null si canceló
    // =========================================================

    public Func<string, string, Task<string?>>?
        SolicitarAutorizacionAdministradorAsync { get; set; }


    // =========================================================
    // VISTA ACTUAL
    // =========================================================

    private ViewModelBase _vistaActual;


    public ViewModelBase VistaActual
    {
        get =>
            _vistaActual;

        private set
        {
            if (
                SetProperty(
                    ref _vistaActual,
                    value
                )
            )
            {
                OnPropertyChanged(
                    nameof(EsInicio)
                );

                OnPropertyChanged(
                    nameof(EsNuevaVenta)
                );

                OnPropertyChanged(
                    nameof(EsPagoCredito)
                );

                OnPropertyChanged(
                    nameof(EsCortes)
                );

                OnPropertyChanged(
                    nameof(EsCancelaciones)
                );

                OnPropertyChanged(
                    nameof(EsReimpresion)
                );

                OnPropertyChanged(
                    nameof(EsConfiguracion)
                );
            }
        }
    }


    // =========================================================
    // INFORMACIÓN GENERAL
    // =========================================================

    [ObservableProperty]
    private string _nombreUsuario =
        "Administrador";


    [ObservableProperty]
    private string _nombreSucursal =
        "Sucursal principal";


    [ObservableProperty]
    private string _tituloPagina =
        "Inicio";


    [ObservableProperty]
    private string _subtituloPagina =
        "Resumen general del punto de venta";


    [ObservableProperty]
    private string _fechaActual =
        DateTime.Now.ToString(
            "dddd, dd 'de' MMMM 'de' yyyy"
        );


    // =========================================================
    // TARJETA DE USUARIO (MENÚ LATERAL)
    // =========================================================

    public string UsuarioActual =>
        string.IsNullOrWhiteSpace(PosSession.NombreUsuario)
            ? "Usuario"
            : PosSession.NombreUsuario;


    public string InicialesUsuario
    {
        get
        {
            var partes =
                UsuarioActual.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

            return partes.Length >= 2
                ? $"{char.ToUpper(partes[0][0])}{char.ToUpper(partes[1][0])}"
                : char.ToUpper(partes[0][0]).ToString();
        }
    }


    public string DetalleUsuario
    {
        get
        {
            var caja =
                string.IsNullOrWhiteSpace(PosSession.NombreCaja)
                    ? PosSession.CodigoCaja
                    : PosSession.NombreCaja;

            var estadoTurno =
                PosSession.TieneTurnoActivo
                    ? "Turno abierto"
                    : "Sin turno";

            return string.IsNullOrWhiteSpace(caja)
                ? estadoTurno
                : $"{caja} · {estadoTurno}";
        }
    }


    public bool TurnoAbierto =>
        PosSession.TieneTurnoActivo;


    public string CajaActual =>
        string.IsNullOrWhiteSpace(PosSession.NombreCaja)
            ? string.IsNullOrWhiteSpace(PosSession.CodigoCaja)
                ? "Sin caja"
                : PosSession.CodigoCaja
            : PosSession.NombreCaja;


    // =========================================================
    // RELOJ DEL ENCABEZADO
    // =========================================================

    [ObservableProperty]
    private string _horaActual =
        DateTime.Now.ToString("HH:mm");


    private readonly DispatcherTimer _reloj;


    // =========================================================
    // ESTADOS DEL MENÚ
    // =========================================================

    public bool EsInicio =>
        VistaActual is InicioViewModel;


    public bool EsNuevaVenta =>
        VistaActual is VentasViewModel;


    public bool EsPagoCredito =>
        VistaActual is PagoCreditoViewModel;


    public bool EsCortes =>
        VistaActual is CortesViewModel;


    public bool EsCancelaciones =>
        VistaActual is CancelacionesViewModel;


    public bool EsReimpresion =>
        VistaActual is ReimpresionViewModel;


    public bool EsConfiguracion =>
        VistaActual is ConfiguracionViewModel;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public MainViewModel()
    {
        InicioModulo =
            new InicioViewModel();


        VentasModulo =
            new VentasViewModel();


        PagoCreditoModulo =
            new PagoCreditoViewModel();


        CortesModulo =
            new CortesViewModel();


        CancelacionesModulo =
            new CancelacionesViewModel();


        ReimpresionModulo =
            new ReimpresionViewModel();


        ConfiguracionModulo =
            new ConfiguracionViewModel();


        // Salir de Reimpresión = volver a Inicio (cierra el acceso admin)
        ReimpresionModulo.Salir =
            () => NavegarCommand.Execute(
                "Inicio"
            );


        // Configuración usa la misma ventana de administrador
        ConfiguracionModulo.SolicitarAutorizacionAdministradorAsync =
            (titulo, descripcion) =>
                SolicitarAutorizacionAdministradorAsync is null
                    ? Task.FromResult<string?>(null)
                    : SolicitarAutorizacionAdministradorAsync(
                        titulo,
                        descripcion);


        // =====================================================
        // NAVEGACIÓN DESDE INICIO
        // =====================================================

        InicioModulo.IrANuevaVenta =
            () => NavegarCommand.Execute(
                "NuevaVenta"
            );


        InicioModulo.IrACortes =
            () => NavegarCommand.Execute(
                "Cortes"
            );


        // =====================================================
        // VISTA INICIAL
        // =====================================================

        _vistaActual =
            InicioModulo;


        // =====================================================
        // RELOJ
        // =====================================================

        _reloj =
            new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15)
            };

        _reloj.Tick +=
            (_, _) =>
            {
                HoraActual =
                    DateTime.Now.ToString("HH:mm");

                FechaActual =
                    DateTime.Now.ToString(
                        "dddd, dd 'de' MMMM 'de' yyyy");
            };

        _reloj.Start();
    }


    // =========================================================
    // NAVEGACIÓN
    // =========================================================

    [RelayCommand]
    private async Task NavegarAsync(
        string? seccion)
    {
        if (
            string.IsNullOrWhiteSpace(
                seccion
            )
        )
        {
            return;
        }


        // =====================================================
        // CERRAR AUTORIZACIÓN DE CANCELACIONES AL SALIR
        // =====================================================
        //
        // Si actualmente estamos en el módulo de cancelaciones
        // y el usuario navega hacia cualquier otro módulo,
        // cerramos únicamente la autorización administrativa
        // temporal de cancelaciones.
        //
        // La sesión principal del usuario/cajero permanece activa.
        // =====================================================

        if (
            VistaActual is CancelacionesViewModel &&
            seccion != "Cancelaciones"
        )
        {
            await CancelacionesModulo
                .CerrarAutorizacionAsync();
        }


        // =====================================================
        // CERRAR AUTORIZACIÓN DE REIMPRESIÓN AL SALIR
        // =====================================================

        if (
            VistaActual is ReimpresionViewModel &&
            seccion != "Reimpresion"
        )
        {
            await ReimpresionModulo
                .CerrarAutorizacionAsync();
        }


        switch (seccion)
        {
            // =================================================
            // INICIO
            // =================================================

            case "Inicio":

                VistaActual =
                    InicioModulo;

                TituloPagina =
                    "Inicio";

                SubtituloPagina =
                    "Resumen general del punto de venta";

                break;


            // =================================================
            // NUEVA VENTA
            // =================================================

            case "NuevaVenta":

                VistaActual =
                    VentasModulo;

                TituloPagina =
                    "Nueva venta";

                SubtituloPagina =
                    "Selecciona o escanea productos para comenzar";

                break;


            // =================================================
            // PAGAR CRÉDITO
            // =================================================

            case "PagoCredito":

                VistaActual =
                    PagoCreditoModulo;

                TituloPagina =
                    "Pagar crédito";

                SubtituloPagina =
                    "Consulta documentos pendientes y registra abonos";

                break;


            // =================================================
            // CORTES
            // =================================================

            case "Cortes":

                VistaActual =
                    CortesModulo;

                TituloPagina =
                    "Cortes y reportes";

                SubtituloPagina =
                    "Consulta aperturas, cierres y resultados de caja";

                break;


            // =================================================
            // CANCELACIONES
            // =================================================

            case "Cancelaciones":

                VistaActual =
                    CancelacionesModulo;

                TituloPagina =
                    "Cancelar ventas";

                SubtituloPagina =
                    "Consulta y cancela ventas finalizadas con autorización administrativa";

                break;


            // =================================================
            // REIMPRESIÓN DE TICKETS (requiere administrador)
            // =================================================

            case "Reimpresion":

                // Ya estamos dentro: no volver a pedir contraseña.
                if (VistaActual is ReimpresionViewModel)
                {
                    break;
                }


                var administrador =
                    SolicitarAutorizacionAdministradorAsync is null
                        ? null
                        : await SolicitarAutorizacionAdministradorAsync(
                            "Autorizar reimpresión de tickets",
                            "Selecciona un administrador e ingresa su contraseña para reimprimir tickets."
                        );


                // Canceló o no pasó la validación: nos quedamos donde estábamos.
                if (string.IsNullOrWhiteSpace(administrador))
                {
                    return;
                }


                VistaActual =
                    ReimpresionModulo;

                TituloPagina =
                    "Reimpresión de tickets";

                SubtituloPagina =
                    "Reimprime tickets de ventas finalizadas con autorización administrativa";

                await ReimpresionModulo
                    .IniciarAsync(administrador);

                break;


            // =================================================
            // CONFIGURACIÓN
            // =================================================

            case "Configuracion":

                ConfiguracionModulo.Cargar();

                VistaActual =
                    ConfiguracionModulo;

                TituloPagina =
                    "Configuración";

                SubtituloPagina =
                    "Impresora, formato de ticket y ajustes de este equipo";

                break;
        }
    }
}