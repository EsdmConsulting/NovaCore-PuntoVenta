using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NovaCoreESDM.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private string _contrasena = string.Empty;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private bool _estaCargando;

    public event Action? InicioSesionExitoso;

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        MensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(Usuario))
        {
            MensajeError = "Ingresa tu usuario.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Contrasena))
        {
            MensajeError = "Ingresa tu contraseña.";
            return;
        }

        EstaCargando = true;

        // Simulación temporal de consulta a la base de datos.
        await Task.Delay(700);

        if (Usuario.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase)
            && Contrasena == "1234")
        {
            InicioSesionExitoso?.Invoke();
        }
        else
        {
            MensajeError = "El usuario o la contraseña son incorrectos.";
        }

        EstaCargando = false;
    }
}