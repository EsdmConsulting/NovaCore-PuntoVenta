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
    private bool
        _cerrandoSesion;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MainWindow()
    {
        InitializeComponent();
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


            /*
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


            /*
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