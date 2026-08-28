using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Services.Auth;

namespace NovaCoreESDM.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private string _contrasena = string.Empty;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private bool _estaCargando;

    public event Action? InicioSesionExitoso;

    public LoginViewModel()
    {
        _authService =
            new AuthService();
    }

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        MensajeError =
            string.Empty;

        if (string.IsNullOrWhiteSpace(Usuario))
        {
            MensajeError =
                "Ingresa tu usuario.";

            return;
        }

        if (string.IsNullOrWhiteSpace(Contrasena))
        {
            MensajeError =
                "Ingresa tu contraseña.";

            return;
        }

        EstaCargando =
            true;

        try
        {
            var resultado =
                await _authService.LoginAsync(
                    Usuario.Trim(),
                    Contrasena
                );

            if (resultado.Res != 1)
            {
                MensajeError =
                    string.IsNullOrWhiteSpace(resultado.Msg)
                        ? "No fue posible iniciar sesión."
                        : resultado.Msg;

                return;
            }

            if (resultado.Data is null)
            {
                MensajeError =
                    "El servidor inició sesión, pero no devolvió los datos del usuario.";

                return;
            }

            // =================================================
            // GUARDAR SESIÓN DEL USUARIO
            // =================================================

            PosSession.IdUsuario =
                resultado.Data.IdUsuario;

            PosSession.NombreUsuario =
                resultado.Data.Nombre;

            PosSession.Usuario =
                resultado.Data.Usuario;

            PosSession.IdRol =
                resultado.Data.IdRol;

            PosSession.Rol =
                resultado.Data.Rol;

            PosSession.EsAdministrador =
                resultado.Data.EsAdministrador;

            PosSession.EsCajero =
                resultado.Data.EsCajero;

            InicioSesionExitoso?.Invoke();
        }
        catch (Exception ex)
        {
            MensajeError =
                $"Ocurrió un error al intentar iniciar sesión: {ex.Message}";
        }
        finally
        {
            EstaCargando =
                false;
        }
    }
}