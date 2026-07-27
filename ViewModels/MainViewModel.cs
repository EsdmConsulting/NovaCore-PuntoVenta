using System;
using CommunityToolkit.Mvvm.ComponentModel;
namespace NovaCoreESDM.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _nombreUsuario = "Administrador";

    [ObservableProperty]
    private string _nombreSucursal = "Sucursal principal";

    [ObservableProperty]
    private string _tituloPagina = "Panel principal";

    [ObservableProperty]
    private string _fechaActual =
        DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy");
}