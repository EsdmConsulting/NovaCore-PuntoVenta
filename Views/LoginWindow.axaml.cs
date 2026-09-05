using System;

using Avalonia.Controls;
using Avalonia.Interactivity;

using NovaCoreESDM.Models.Session;

using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.Services.Printing;
using NovaCoreESDM.Services.Turnos;

using NovaCoreESDM.ViewModels;

using NovaCoreESDM.Views.Caja;
using NovaCoreESDM.Views.Turno;

namespace NovaCoreESDM.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel
        _viewModel;

    private readonly TerminalConfigurationService
        _terminalConfigurationService;

    private readonly TurnosService
        _turnosService;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public LoginWindow()
    {
        InitializeComponent();


        _viewModel =
            new LoginViewModel();


        _terminalConfigurationService =
            new TerminalConfigurationService();


        _turnosService =
            new TurnosService();


        DataContext =
            _viewModel;


        _viewModel.InicioSesionExitoso +=
            OnInicioSesionExitoso;
    }


    // =========================================================
    // INICIO DE SESIÓN CORRECTO
    // =========================================================

    private async void OnInicioSesionExitoso()
    {
        try
        {
            // =================================================
            // 1. OBTENER CONFIGURACIÓN LOCAL DE ESTA TERMINAL
            // =================================================

            var configuracion =
                await _terminalConfigurationService
                    .ObtenerConfiguracionAsync();


            // =================================================
            // 2. ESTA PC TODAVÍA NO TIENE CAJA CONFIGURADA
            // =================================================

            if (configuracion is null)
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "ESTA TERMINAL NO TIENE CAJA CONFIGURADA.");

                Console.WriteLine(
                    "Se abrirá la selección de caja.");

                Console.WriteLine(
                    "====================================");


                var seleccionarCajaWindow =
                    new SeleccionarCajaWindow();


                seleccionarCajaWindow.Show();


                Close();

                return;
            }


            // =================================================
            // 3. GUARDAR CONTEXTO DE LA TERMINAL EN POSSESSION
            // =================================================

            /*
             * La caja pertenece a esta computadora.
             *
             * No importa qué usuario haya iniciado sesión:
             * la terminal conserva su caja configurada.
             */

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


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "TERMINAL CONFIGURADA:");

            Console.WriteLine(
                $"IdEmpresa: {PosSession.IdEmpresa}");

            Console.WriteLine(
                $"IdUnidadOperativa: {PosSession.IdUnidadOperativa}");

            Console.WriteLine(
                $"IdCaja: {PosSession.IdCaja}");

            Console.WriteLine(
                $"CodigoCaja: {PosSession.CodigoCaja}");

            Console.WriteLine(
                $"NombreCaja: {PosSession.NombreCaja}");

            Console.WriteLine(
                $"IdTurnoActivo local: {configuracion.IdTurnoActivo}");

            Console.WriteLine(
                "====================================");


            // =================================================
            // 4. CONSULTAR TURNO REAL DE LA CAJA EN SERVIDOR
            // =================================================

            /*
             * IMPORTANTE:
             *
             * IdTurnoActivo del archivo local es únicamente
             * una referencia/cache.
             *
             * La verdad sobre si existe un turno ABIERTO
             * está en PostgreSQL.
             */

            var resultadoTurno =
                await _turnosService
                    .ObtenerTurnoActualAsync(
                        configuracion.IdCaja);


            // =================================================
            // 5. ERROR CONSULTANDO EL TURNO
            // =================================================

            /*
             * NO debemos interpretar un error del servidor
             * como "no existe turno".
             *
             * Si no pudimos verificar el turno,
             * nos quedamos en Login.
             */

            if (resultadoTurno.Res != 1)
            {
                PosSession.LimpiarTurno();


                _viewModel.MensajeError =
                    string.IsNullOrWhiteSpace(
                        resultadoTurno.Msg)

                        ? "No fue posible verificar el turno actual de esta caja."

                        : resultadoTurno.Msg;


                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "ERROR CONSULTANDO TURNO:");

                Console.WriteLine(
                    _viewModel.MensajeError);

                Console.WriteLine(
                    "====================================");


                return;
            }


            // =================================================
            // 6. EXISTE UN TURNO ABIERTO
            // =================================================

            if (
                resultadoTurno.Data is not null &&
                resultadoTurno.Data.Id > 0
            )
            {
                var turno =
                    resultadoTurno.Data;


                // =============================================
                // ESTABLECER TURNO EN LA SESIÓN ACTUAL
                // =============================================

                PosSession.IdTurno =
                    turno.Id;


                // =============================================
                // SINCRONIZAR ARCHIVO LOCAL
                // =============================================

                /*
                 * Si el archivo decía:
                 *
                 * IdTurnoActivo = null
                 *
                 * o incluso tenía otro turno viejo,
                 * lo corregimos con el valor real del servidor.
                 */

                await _terminalConfigurationService
                    .GuardarTurnoActivoAsync(
                        turno.Id);


                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "TURNO ABIERTO RECUPERADO DEL SERVIDOR:");

                Console.WriteLine(
                    $"IdTurno: {turno.Id}");

                Console.WriteLine(
                    $"IdCaja: {turno.IdCaja}");

                Console.WriteLine(
                    $"Caja: {turno.CajaNombre}");

                Console.WriteLine(
                    $"CodigoCaja: {turno.CajaCodigo}");

                Console.WriteLine(
                    $"Estado: {turno.Estado}");

                Console.WriteLine(
                    $"Usuario apertura: {turno.UsuarioAperturaNombre}");

                Console.WriteLine(
                    $"Fondo inicial: {turno.FondoInicial}");

                Console.WriteLine(
                    $"Fecha apertura: {turno.FechaApertura}");

                Console.WriteLine(
                    "====================================");


                // =============================================
                // ENTRAR DIRECTAMENTE AL POS
                // =============================================

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


            // =================================================
            // 7. NO EXISTE TURNO ABIERTO
            // =================================================

            /*
             * Si llegamos aquí:
             *
             * resultadoTurno.Res == 1
             * resultadoTurno.Data == null
             *
             * Es decir:
             *
             * PostgreSQL confirmó correctamente que esta caja
             * NO tiene ningún turno ABIERTO.
             */


            PosSession.LimpiarTurno();


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "NO EXISTE TURNO ABIERTO.");

            Console.WriteLine(
                $"IdCaja: {configuracion.IdCaja}");

            Console.WriteLine(
                $"Caja: {configuracion.NombreCaja}");

            Console.WriteLine(
                $"Turno guardado localmente: {configuracion.IdTurnoActivo}");

            Console.WriteLine(
                "Se solicitará apertura de un nuevo turno.");

            Console.WriteLine(
                "====================================");


            // =================================================
            // 8. ABRIR PANTALLA DE APERTURA
            // =================================================

            var aperturaTurnoWindow =
                new AperturaTurnoWindow();


            aperturaTurnoWindow.Show();


            Close();
        }
        catch (Exception ex)
        {
            // =================================================
            // ERROR GENERAL
            // =================================================

            PosSession.LimpiarTurno();


            _viewModel.MensajeError =
                $"No fue posible preparar la terminal: {ex.Message}";


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR PREPARANDO TERMINAL:");

            Console.WriteLine(
                ex);

            Console.WriteLine(
                "====================================");
        }
    }


    // =========================================================
    // TEMPORAL: PRUEBA DEL CAJÓN DE EFECTIVO
    // =========================================================

    private async void AbrirCajonPrueba_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            var printerService =
                new WindowsRawPrinterService();


            var resultado =
                await printerService
                    .AbrirCajonAsync();


            if (resultado)
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "COMANDO DE APERTURA ENVIADO AL CAJÓN");

                Console.WriteLine(
                    "====================================");
            }
            else
            {
                Console.WriteLine(
                    "La impresora no confirmó el comando del cajón.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR AL ABRIR CAJÓN:");

            Console.WriteLine(
                ex.Message);

            Console.WriteLine(
                "====================================");
        }
    }


    // =========================================================
    // LIMPIEZA
    // =========================================================

    protected override void OnClosed(
        EventArgs e)
    {
        _viewModel.InicioSesionExitoso -=
            OnInicioSesionExitoso;


        base.OnClosed(e);
    }
}