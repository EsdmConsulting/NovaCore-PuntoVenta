using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

using NovaCoreESDM.Models.Session;
using NovaCoreESDM.ViewModels;
using NovaCoreESDM.Views.Sesion;

namespace NovaCoreESDM.Views;

public partial class MainWindow
    : Window
{
    // ============================================================
    // CONTROLES DEL SIDEBAR
    // ============================================================

    private Grid _mainLayout = null!;

    private Button _toggleSidebarButton = null!;

    private StackPanel _identidadExpandida = null!;
    private StackPanel _identidadColapsada = null!;

    private TextBlock _operacionTextBlock = null!;

    private TextBlock _nuevaVentaText = null!;
    private TextBlock _resumenText = null!;
    private TextBlock _pagarCreditoText = null!;
    private TextBlock _cortesText = null!;
    private TextBlock _cancelarVentasText = null!;
    private TextBlock _cerrarSesionText = null!;


    // ============================================================
    // ESTADO
    // ============================================================

    private bool
        _cerrandoSesion;

    private bool
        _sidebarColapsado;


    // ============================================================
    // TAMAÑOS
    // ============================================================

    private const double SidebarExpandido =
        220;

    private const double SidebarColapsado =
        72;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MainWindow()
    {
        InitializeComponent();


        // ========================================================
        // OBTENER CONTROLES DEL XAML
        // ========================================================

        _mainLayout =
            this.FindControl<Grid>(
                "MainLayout")
            ?? throw new InvalidOperationException(
                "No se encontró MainLayout.");


        _toggleSidebarButton =
            this.FindControl<Button>(
                "ToggleSidebarButton")
            ?? throw new InvalidOperationException(
                "No se encontró ToggleSidebarButton.");


        _identidadExpandida =
            this.FindControl<StackPanel>(
                "IdentidadExpandida")
            ?? throw new InvalidOperationException(
                "No se encontró IdentidadExpandida.");


        _identidadColapsada =
            this.FindControl<StackPanel>(
                "IdentidadColapsada")
            ?? throw new InvalidOperationException(
                "No se encontró IdentidadColapsada.");


        _operacionTextBlock =
            this.FindControl<TextBlock>(
                "OperacionTextBlock")
            ?? throw new InvalidOperationException(
                "No se encontró OperacionTextBlock.");


        _nuevaVentaText =
            this.FindControl<TextBlock>(
                "NuevaVentaText")
            ?? throw new InvalidOperationException(
                "No se encontró NuevaVentaText.");


        _resumenText =
            this.FindControl<TextBlock>(
                "ResumenText")
            ?? throw new InvalidOperationException(
                "No se encontró ResumenText.");


        _pagarCreditoText =
            this.FindControl<TextBlock>(
                "PagarCreditoText")
            ?? throw new InvalidOperationException(
                "No se encontró PagarCreditoText.");


        _cortesText =
            this.FindControl<TextBlock>(
                "CortesText")
            ?? throw new InvalidOperationException(
                "No se encontró CortesText.");


        _cancelarVentasText =
            this.FindControl<TextBlock>(
                "CancelarVentasText")
            ?? throw new InvalidOperationException(
                "No se encontró CancelarVentasText.");


        _cerrarSesionText =
            this.FindControl<TextBlock>(
                "CerrarSesionText")
            ?? throw new InvalidOperationException(
                "No se encontró CerrarSesionText.");


        // ========================================================
        // ESTADO INICIAL
        // ========================================================

        _sidebarColapsado =
            false;

        ActualizarSidebar();
    }


    // ============================================================
    // COLAPSAR / EXPANDIR MENÚ
    // ============================================================

    private void ToggleSidebar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _sidebarColapsado =
            !_sidebarColapsado;

        ActualizarSidebar();
    }


    // ============================================================
    // NUEVA VENTA
    // ============================================================
    //
    // El Command sigue haciendo la navegación.
    //
    // Este evento solamente colapsa el sidebar para darle
    // más espacio al área de productos.
    // ============================================================

    private void NuevaVenta_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_sidebarColapsado)
        {
            return;
        }


        _sidebarColapsado =
            true;

        ActualizarSidebar();
    }


    // ============================================================
    // ACTUALIZAR SIDEBAR
    // ============================================================

    private void ActualizarSidebar()
    {
        if (_sidebarColapsado)
        {
            // ====================================================
            // MENÚ COLAPSADO
            // ====================================================

            _mainLayout
                .ColumnDefinitions[0]
                .Width =
                new GridLength(
                    SidebarColapsado);


            _toggleSidebarButton.Content =
                "›";


            // ====================================================
            // LOGO / IDENTIDAD
            // ====================================================

            _identidadExpandida.IsVisible =
                false;

            _identidadColapsada.IsVisible =
                true;


            // ====================================================
            // TÍTULOS
            // ====================================================

            _operacionTextBlock.IsVisible =
                false;


            // ====================================================
            // TEXTOS DE MENÚ
            // ====================================================

            _nuevaVentaText.IsVisible =
                false;

            _resumenText.IsVisible =
                false;

            _pagarCreditoText.IsVisible =
                false;

            _cortesText.IsVisible =
                false;

            _cancelarVentasText.IsVisible =
                false;

            _cerrarSesionText.IsVisible =
                false;
        }
        else
        {
            // ====================================================
            // MENÚ EXPANDIDO
            // ====================================================

            _mainLayout
                .ColumnDefinitions[0]
                .Width =
                new GridLength(
                    SidebarExpandido);


            _toggleSidebarButton.Content =
                "‹";


            // ====================================================
            // LOGO / IDENTIDAD
            // ====================================================

            _identidadExpandida.IsVisible =
                true;

            _identidadColapsada.IsVisible =
                false;


            // ====================================================
            // TÍTULOS
            // ====================================================

            _operacionTextBlock.IsVisible =
                true;


            // ====================================================
            // TEXTOS DE MENÚ
            // ====================================================

            _nuevaVentaText.IsVisible =
                true;

            _resumenText.IsVisible =
                true;

            _pagarCreditoText.IsVisible =
                true;

            _cortesText.IsVisible =
                true;

            _cancelarVentasText.IsVisible =
                true;

            _cerrarSesionText.IsVisible =
                true;
        }
    }


    // ============================================================
    // BOTÓN CERRAR SESIÓN
    // ============================================================

    private async void CerrarSesion_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_cerrandoSesion)
        {
            return;
        }


        // ========================================================
        // MOSTRAR VENTANA SEGÚN EL ESTADO DEL TURNO
        // ========================================================

        var ventana =
            new CerrarSesionWindow(
                PosSession.TieneTurnoActivo,
                PosSession.IdTurno,
                PosSession.NombreCaja);


        var resultado =
            await ventana
                .ShowDialog<CerrarSesionWindow.ResultadoCerrarSesion>(
                    this);


        // ========================================================
        // VOLVER
        // ========================================================

        if (resultado ==
            CerrarSesionWindow.ResultadoCerrarSesion.Cancelar)
        {
            return;
        }


        // ========================================================
        // IR A REALIZAR CORTE
        // ========================================================

        if (resultado ==
            CerrarSesionWindow.ResultadoCerrarSesion.IrACorte)
        {
            IrACortes();

            return;
        }


        // ========================================================
        // CERRAR SESIÓN
        // ========================================================

        if (resultado ==
            CerrarSesionWindow.ResultadoCerrarSesion.CerrarSesion)
        {
            await CerrarSesionAsync();
        }
    }


    // ============================================================
    // NAVEGAR A CORTES
    // ============================================================

    private void IrACortes()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }


        if (!viewModel.NavegarCommand
                .CanExecute(
                    "Cortes"))
        {
            return;
        }


        viewModel.NavegarCommand
            .Execute(
                "Cortes");
    }


    // ============================================================
    // CERRAR SESIÓN
    // ============================================================

    private async Task CerrarSesionAsync()
    {
        if (_cerrandoSesion)
        {
            return;
        }


        try
        {
            _cerrandoSesion =
                true;


            /**
             * MUY IMPORTANTE:
             *
             * Cerrar sesión NO significa cerrar turno.
             *
             * Si existe un turno ABIERTO:
             *
             * - NO llamamos cerrar.php
             * - NO modificamos PostgreSQL
             * - NO borramos IdTurnoActivo del archivo local
             * - NO eliminamos la configuración de caja
             *
             * Únicamente desaparece el usuario actual de memoria.
             */


            // ====================================================
            // 1. LIMPIAR SESIÓN EN MEMORIA
            // ====================================================

            PosSession
                .LimpiarSesion();


            // ====================================================
            // 2. CREAR LOGIN
            // ====================================================

            var loginWindow =
                new LoginWindow();


            // ====================================================
            // 3. ESTABLECER LOGIN COMO VENTANA PRINCIPAL
            // ====================================================

            if (Application.Current?
                    .ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow =
                    loginWindow;
            }


            // ====================================================
            // 4. MOSTRAR LOGIN
            // ====================================================

            loginWindow
                .Show();


            /**
             * Primero mostramos Login y DESPUÉS cerramos
             * MainWindow.
             *
             * De esta forma nunca dejamos la aplicación
             * sin ninguna ventana abierta.
             */


            // ====================================================
            // 5. CERRAR MAINWINDOW
            // ====================================================

            Close();


            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _cerrandoSesion =
                false;


            Console.WriteLine(
                "====================================");


            Console.WriteLine(
                "ERROR AL CERRAR SESIÓN:");


            Console.WriteLine(
                ex);


            Console.WriteLine(
                "====================================");
        }
    }
}