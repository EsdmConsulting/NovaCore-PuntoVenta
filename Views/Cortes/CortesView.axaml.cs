using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.Services.Printing;
using NovaCoreESDM.ViewModels.Cortes;

using Avalonia.Controls.ApplicationLifetimes;

using NovaCoreESDM.Views;
using NovaCoreESDM.Views.Turno;
using NovaCoreESDM.Views.Comun;

namespace NovaCoreESDM.Views.Cortes;

public partial class CortesView
    : UserControl
{
    // ============================================================
    // SERVICIOS
    // ============================================================

    private readonly TerminalConfigurationService
        _terminalConfigurationService =
            new();


    private readonly WindowsRawPrinterService
        _printerService =
            new();


    private bool
        _cargaInicialRealizada;


    private CortesViewModel? ViewModel =>
        DataContext as CortesViewModel;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public CortesView()
    {
        AvaloniaXamlLoader.Load(
            this);


        AttachedToVisualTree +=
            CortesView_AttachedToVisualTree;


        // La escena animada del banner solo se muestra cuando hay
        // espacio suficiente; en pantallas angostas se vería diminuta.
        var banner =
            this.FindControl<Border>("BannerCorte");

        var escena =
            this.FindControl<Control>("EscenaCorte");

        if (banner is not null && escena is not null)
        {
            banner.SizeChanged +=
                (_, e) =>
                    escena.IsVisible =
                        e.NewSize.Width >= 1050;
        }
    }


    // ============================================================
    // CARGA INICIAL
    // ============================================================

    private async void CortesView_AttachedToVisualTree(
        object? sender,
        VisualTreeAttachmentEventArgs e)
    {
        if (_cargaInicialRealizada)
        {
            return;
        }


        if (ViewModel is null)
        {
            return;
        }


        _cargaInicialRealizada =
            true;


        ViewModel.RealizarCorteSolicitado +=
            ViewModel_RealizarCorteSolicitado;


        await ViewModel
            .CargarAsync();
    }


    // ============================================================
    // REALIZAR CORTE
    // ============================================================

    private void RealizarCorte_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ViewModel?
            .SolicitarRealizarCorte();
    }


    // =========================================================
    // CORTE SIN CONEXIÓN (DIFERIDO)
    // =========================================================

    private async Task RealizarCorteDiferidoAsync(Window owner)
    {
        if (PosSession.IdTurno <= 0)
        {
            await MostrarAvisoAsync(
                owner,
                "No hay un turno abierto",
                "No existe un turno activo para realizar el corte.");
            return;
        }

        // El cajón se abre para contar, igual que en el corte con internet.
        try
        {
            if (OperatingSystem.IsWindows())
                await _printerService.AbrirCajonAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo abrir el cajón para el corte sin conexión: {ex.Message}");
        }

        var ventana = new CorteDiferidoWindow(PosSession.IdTurno);

        if (!await ventana.ShowDialog<bool>(owner) || ventana.Corte is null)
            return;

        // El turno ya no se usa en esta caja: el servidor lo cierra al
        // regresar el internet. Para vender se necesita abrir otro turno.
        PosSession.LimpiarTurno();
        await _terminalConfigurationService.LimpiarTurnoActivoAsync();
        ViewModel?.EstablecerSinTurno();

        await MostrarAvisoAsync(
            owner,
            "Corte guardado sin conexión",
            "El corte quedó guardado en la caja y se imprimió el pre-corte provisional.\n\n" +
            "Cuando regrese el internet se enviarán las ventas y abonos del turno y se cerrará " +
            "el corte oficial con la hora de este conteo. Podrás consultarlo y reimprimirlo en Cortes.\n\n" +
            "Para seguir vendiendo se necesita internet para abrir un turno nuevo.");

        PosSession.LimpiarSesion();

        var loginWindow = new LoginWindow();

        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = loginWindow;
        }

        loginWindow.Show();
        owner.Close();
    }


    private async void ViewModel_RealizarCorteSolicitado(
        object? sender,
        EventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }


        // ========================================================
        // OBTENER VENTANA PRINCIPAL
        // ========================================================

        var owner =
            TopLevel.GetTopLevel(this)
                as Window;


        if (owner is null)
        {
            return;
        }


        // Sin internet: corte diferido (se cierra en el servidor después).
        if (!NovaCoreESDM.Services.Offline.ConexionService.EstaEnLinea)
        {
            await RealizarCorteDiferidoAsync(owner);
            return;
        }


        // ========================================================
        // ACTUALIZAR INFORMACIÓN DEL TURNO
        // ========================================================
        //
        // Muy importante:
        //
        // Volvemos a consultar al servidor justo antes
        // de iniciar el corte.
        //
        // De esta manera no dependemos de información vieja
        // que pudiera estar mostrando el dashboard.
        // ========================================================

        await ViewModel
            .CargarAsync();


        var resumen =
            ViewModel.ResumenActual;

        // Se fue el internet al consultar el turno: corte diferido.
        if (resumen is null && !NovaCoreESDM.Services.Offline.ConexionService.EstaEnLinea)
        {
            await RealizarCorteDiferidoAsync(owner);
            return;
        }


        // ========================================================
        // VALIDAR QUE EXISTA TURNO
        // ========================================================

        if (resumen is null ||
            resumen.Turno is null)
        {
            await MostrarAvisoAsync(
                owner,
                "No hay un turno abierto",
                "No existe un turno activo para realizar el corte.");

            return;
        }


        // ========================================================
        // VALIDAR ESTADO
        // ========================================================

        if (!string.Equals(
                resumen.Turno.Estado,
                "ABIERTO",
                StringComparison.OrdinalIgnoreCase))
        {
            await MostrarAvisoAsync(
                owner,
                "Turno no disponible",
                "El turno actual ya no se encuentra abierto.");

            return;
        }


        // ========================================================
        // VALIDAR SI EL BACKEND PERMITE CERRAR
        // ========================================================
        //
        // Normalmente esto será FALSE cuando exista
        // una venta BORRADOR pendiente.
        // ========================================================

        if (!resumen.Turno.PuedeCerrar)
        {
            await MostrarAvisoAsync(
                owner,
                "Venta pendiente",
                "Hay una venta pendiente en el carrito.\n\n" +
                "Finaliza o elimina la venta antes de realizar el corte.");

            return;
        }


        // ========================================================
        // ABRIR CAJÓN AUTOMÁTICAMENTE
        // ========================================================
        //
        // El cajón se abre ANTES de mostrar la ventana
        // donde el cajero captura el efectivo contado.
        //
        // Si la impresora o el cajón fallan, NO se bloquea
        // el proceso de corte.
        // ========================================================

        var cajonAbierto =
            false;


        try
        {
            // El servicio utiliza el spooler RAW de Windows.
            if (OperatingSystem.IsWindows())
            {
                cajonAbierto =
                    await _printerService
                        .AbrirCajonAsync();
            }
            else
            {
                Console.WriteLine(
                    "Apertura automática de cajón omitida: " +
                    "el sistema actual no es Windows.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "ERROR ABRIENDO CAJÓN PARA CORTE:");

            Console.WriteLine(
                ex);

            Console.WriteLine(
                "========================================");
        }


        // ========================================================
        // SI NO ABRIÓ, AVISAR PERO CONTINUAR
        // ========================================================

        if (!cajonAbierto)
        {
            await MostrarAvisoAsync(
                owner,
                "Abrir cajón manualmente",
                "No fue posible abrir el cajón automáticamente.\n\n" +
                "Ábrelo manualmente con la llave para contar " +
                "el efectivo y continuar con el corte.");
        }


        // ========================================================
        // ABRIR PANTALLA DE CORTE
        // ========================================================
        //
        // Aquí el cajero:
        //
        // 1. Cuenta el dinero físicamente.
        // 2. Captura el efectivo contado.
        // 3. Revisa diferencia.
        // 4. Confirma el cierre.
        //
        // Todavía NO cerramos el turno aquí.
        // ========================================================

        var ventana =
            new RealizarCorteWindow(
                resumen);


        var realizado =
            await ventana
                .ShowDialog<bool>(
                    owner);


        // ========================================================
        // VALIDAR RESULTADO
        // ========================================================

        if (!realizado ||
            !ventana.CorteRealizadoCorrectamente)
        {
            return;
        }


        // ========================================================
        // EL BACKEND YA CERRÓ EL TURNO
        // ========================================================

        PosSession
            .LimpiarTurno();


        await _terminalConfigurationService
            .LimpiarTurnoActivoAsync();


        // ========================================================
        // ACTUALIZAR INTERFAZ
        // ========================================================

        ViewModel
            .EstablecerSinTurno();


        await ViewModel
            .CargarHistorialAsync();


        // ========================================================
        // ACCIÓN ELEGIDA DESPUÉS DEL CORTE
        // ========================================================

        switch (ventana.AccionPosterior)
        {
            // ====================================================
            // ABRIR NUEVO TURNO
            // ====================================================

            case AccionDespuesCorte.AbrirNuevoTurno:
            {
                var aperturaTurnoWindow =
                    new AperturaTurnoWindow();


                // Si utilizamos ClassicDesktop,
                // dejamos esta como la ventana principal activa.
                if (Application.Current?.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow =
                        aperturaTurnoWindow;
                }


                aperturaTurnoWindow
                    .Show();


                owner
                    .Close();


                break;
            }


            // ====================================================
            // CERRAR SESIÓN
            // ====================================================

            case AccionDespuesCorte.CerrarSesion:
            {
                /*
                 * IMPORTANTE:
                 *
                 * Limpiamos usuario / rol / turno,
                 * pero NO borramos la configuración
                 * de la caja de esta computadora.
                 */

                PosSession
                    .LimpiarSesion();


                var loginWindow =
                    new LoginWindow();


                if (Application.Current?.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow =
                        loginWindow;
                }


                loginWindow
                    .Show();


                owner
                    .Close();


                break;
            }


            // ====================================================
            // NO SE ELIGIÓ NINGUNA ACCIÓN
            // ====================================================

            default:
            {
                /*
                 * Por ejemplo:
                 * si en algún momento permitimos cerrar
                 * CorteRealizadoWindow con la X.
                 *
                 * Nos quedamos en MainWindow,
                 * pero ya sin turno activo.
                 */

                break;
            }
        }
    }


    // ============================================================
    // MOSTRAR AVISO
    // ============================================================
    //
    // Ventana sencilla utilizada para explicar al cajero
    // por qué no puede continuar.
    //
    // Usa AvisoWindow (Views/Comun), reutilizable en todo NovaCore.
    // ============================================================

    private static async Task MostrarAvisoAsync(
        Window owner,
        string titulo,
        string mensaje)
    {
        var ventanaAviso =
            new AvisoWindow(
                titulo,
                mensaje);


        await ventanaAviso
            .ShowDialog(
                owner);
    }


    // ============================================================
    // ACTUALIZAR
    // ============================================================

    private async void Actualizar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await ActualizarAsync();
    }


    private async Task ActualizarAsync()
    {
        if (ViewModel is null)
        {
            return;
        }


        await ViewModel
            .CargarAsync();
    }
}