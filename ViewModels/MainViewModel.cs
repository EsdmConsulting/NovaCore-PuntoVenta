using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NovaCoreESDM.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _nombreUsuario = "Administrador";

    [ObservableProperty]
    private string _nombreSucursal = "Sucursal principal";

    [ObservableProperty]
    private string _tituloPagina = "Inicio";

    [ObservableProperty]
    private string _subtituloPagina = "Resumen general del punto de venta";

    [ObservableProperty]
    private string _fechaActual =
        DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy");

    [ObservableProperty]
    private string _seccionActual = "Inicio";

    public bool EsInicio => SeccionActual == "Inicio";

    public bool EsNuevaVenta => SeccionActual == "NuevaVenta";

    public bool EsCortes => SeccionActual == "Cortes";

    [RelayCommand]
    private void Navegar(string? seccion)
    {
        if (string.IsNullOrWhiteSpace(seccion))
            return;

        SeccionActual = seccion;

        switch (seccion)
        {
            case "Inicio":
                TituloPagina = "Inicio";
                SubtituloPagina = "Resumen general del punto de venta";
                break;

            case "NuevaVenta":
                TituloPagina = "Nueva venta";
                SubtituloPagina = "Selecciona o escanea productos para comenzar";
                break;

            case "Cortes":
                TituloPagina = "Cortes y reportes";
                SubtituloPagina = "Consulta aperturas, cierres y resultados de caja";
                break;

            default:
                SeccionActual = "Inicio";
                TituloPagina = "Inicio";
                SubtituloPagina = "Resumen general del punto de venta";
                break;
        }

        OnPropertyChanged(nameof(EsInicio));
        OnPropertyChanged(nameof(EsNuevaVenta));
        OnPropertyChanged(nameof(EsCortes));
    }
}