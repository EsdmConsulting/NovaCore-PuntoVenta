using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.ViewModels.Cortes;
using NovaCoreESDM.ViewModels.Inicio;
using NovaCoreESDM.ViewModels.Ventas;

namespace NovaCoreESDM.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // Instancias de cada módulo.
    public InicioViewModel InicioModulo { get; }

    public VentasViewModel VentasModulo { get; }

    public CortesViewModel CortesModulo { get; }

    private ViewModelBase _vistaActual;

    public ViewModelBase VistaActual
    {
        get => _vistaActual;

        private set
        {
            if (SetProperty(ref _vistaActual, value))
            {
                OnPropertyChanged(nameof(EsInicio));
                OnPropertyChanged(nameof(EsNuevaVenta));
                OnPropertyChanged(nameof(EsCortes));
            }
        }
    }

    [ObservableProperty]
    private string _nombreUsuario = "Administrador";

    [ObservableProperty]
    private string _nombreSucursal = "Sucursal principal";

    [ObservableProperty]
    private string _tituloPagina = "Inicio";

    [ObservableProperty]
    private string _subtituloPagina =
        "Resumen general del punto de venta";

    [ObservableProperty]
    private string _fechaActual =
        DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy");

    public bool EsInicio =>
        VistaActual is InicioViewModel;

    public bool EsNuevaVenta =>
        VistaActual is VentasViewModel;

    public bool EsCortes =>
        VistaActual is CortesViewModel;

    public MainViewModel()
    {
        InicioModulo = new InicioViewModel();
        VentasModulo = new VentasViewModel();
        CortesModulo = new CortesViewModel();

        _vistaActual = InicioModulo;
    }

    [RelayCommand]
    private void Navegar(string? seccion)
    {
        if (string.IsNullOrWhiteSpace(seccion))
            return;

        switch (seccion)
        {
            case "Inicio":
                VistaActual = InicioModulo;
                TituloPagina = "Inicio";
                SubtituloPagina =
                    "Resumen general del punto de venta";
                break;

            case "NuevaVenta":
                VistaActual = VentasModulo;
                TituloPagina = "Nueva venta";
                SubtituloPagina =
                    "Selecciona o escanea productos para comenzar";
                break;

            case "Cortes":
                VistaActual = CortesModulo;
                TituloPagina = "Cortes y reportes";
                SubtituloPagina =
                    "Consulta aperturas, cierres y resultados de caja";
                break;
        }
    }
}