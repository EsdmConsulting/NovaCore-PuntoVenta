using System;
using Avalonia.Controls;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.ViewModels;
using NovaCoreESDM.Views.Caja;
using NovaCoreESDM.Views.Turno;

namespace NovaCoreESDM.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    private readonly TerminalConfigurationService
        _terminalConfigurationService;

    public LoginWindow()
    {
        InitializeComponent();

        _viewModel =
            new LoginViewModel();

        _terminalConfigurationService =
            new TerminalConfigurationService();

        DataContext =
            _viewModel;

        _viewModel.InicioSesionExitoso +=
            OnInicioSesionExitoso;
    }

    private async void OnInicioSesionExitoso()
    {
        try
        {
            // =====================================================
            // 1. OBTENER CONFIGURACIÓN LOCAL DE ESTA TERMINAL
            // =====================================================

            var configuracion =
                await _terminalConfigurationService
                    .ObtenerConfiguracionAsync();


            // =====================================================
            // 2. ESTA PC TODAVÍA NO TIENE CAJA CONFIGURADA
            // =====================================================

            if (configuracion is null)
            {
                var seleccionarCajaWindow =
                    new SeleccionarCajaWindow();

                seleccionarCajaWindow.Show();

                Close();

                return;
            }


            // =====================================================
            // 3. GUARDAR CONTEXTO DE LA CAJA EN POSSESSION
            // =====================================================

            PosSession.IdEmpresa =
                configuracion.IdEmpresa;

            PosSession.IdUnidadOperativa =
                configuracion.IdUnidadOperativa;

            PosSession.IdCaja =
                configuracion.IdCaja;

            PosSession.CodigoCaja =
                configuracion.CodigoCaja;

            PosSession.NombreCaja =
                configuracion.NombreCaja;


            // =====================================================
            // 4. YA EXISTE UN TURNO ACTIVO GUARDADO LOCALMENTE
            // =====================================================

            if (configuracion.IdTurnoActivo.HasValue &&
                configuracion.IdTurnoActivo.Value > 0)
            {
                PosSession.IdTurno =
                    configuracion.IdTurnoActivo.Value;


                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "TURNO LOCAL RECUPERADO:");

                Console.WriteLine(
                    $"IdTurno: {PosSession.IdTurno}");

                Console.WriteLine(
                    $"IdCaja: {PosSession.IdCaja}");

                Console.WriteLine(
                    $"Caja: {PosSession.NombreCaja}");

                Console.WriteLine(
                    "====================================");


                var mainWindow =
                    new MainWindow
                    {
                        DataContext =
                            new MainViewModel()
                    };

                mainWindow.Show();

                Close();

                return;
            }


            // =====================================================
            // 5. NO HAY TURNO ACTIVO LOCAL
            // =====================================================

            PosSession.LimpiarTurno();


            var aperturaTurnoWindow =
                new AperturaTurnoWindow();

            aperturaTurnoWindow.Show();

            Close();
        }
        catch (Exception ex)
        {
            _viewModel.MensajeError =
                $"No fue posible preparar la terminal: {ex.Message}";
        }
    }


    protected override void OnClosed(
        EventArgs e)
    {
        _viewModel.InicioSesionExitoso -=
            OnInicioSesionExitoso;

        base.OnClosed(e);
    }
}