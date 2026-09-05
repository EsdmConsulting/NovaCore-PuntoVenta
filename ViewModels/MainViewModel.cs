using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using NovaCoreESDM.ViewModels.Cortes;
using NovaCoreESDM.ViewModels.Credito;
using NovaCoreESDM.ViewModels.Inicio;
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


        // =====================================================
        // NAVEGACIÓN DESDE INICIO
        // =====================================================

        InicioModulo.IrANuevaVenta =
            () => Navegar(
                "NuevaVenta"
            );


        InicioModulo.IrACortes =
            () => Navegar(
                "Cortes"
            );


        // =====================================================
        // VISTA INICIAL
        // =====================================================

        _vistaActual =
            InicioModulo;
    }


    // =========================================================
    // NAVEGACIÓN
    // =========================================================

    [RelayCommand]
    private void Navegar(
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
        }
    }
}