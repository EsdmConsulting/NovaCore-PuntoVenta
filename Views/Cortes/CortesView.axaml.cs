using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.ViewModels.Cortes;

namespace NovaCoreESDM.Views.Cortes;

public partial class CortesView
    : UserControl
{
    private readonly TerminalConfigurationService
        _terminalConfigurationService =
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


    private async void ViewModel_RealizarCorteSolicitado(
        object? sender,
        EventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }


        // ========================================================
        // OBTENER RESUMEN ACTUAL
        // ========================================================

        var resumen =
            ViewModel.ResumenActual;


        // Si por alguna razón todavía no está cargado,
        // volvemos a consultar el turno.
        if (resumen is null)
        {
            await ViewModel
                .CargarAsync();


            resumen =
                ViewModel.ResumenActual;
        }


        if (resumen is null)
        {
            return;
        }


        // ========================================================
        // VALIDAR TURNO
        // ========================================================

        if (resumen.Turno is null)
        {
            return;
        }


        if (!string.Equals(
                resumen.Turno.Estado,
                "ABIERTO",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }


        if (!resumen.Turno.PuedeCerrar)
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


        // ========================================================
        // ABRIR PANTALLA DE CORTE
        //
        // Aquí el cajero captura el efectivo contado físicamente.
        // NO se cierra todavía el turno.
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
        //
        // RealizarCorteWindow internamente:
        //
        // 1. Captura efectivo contado
        // 2. Muestra diferencia
        // 3. Abre ConfirmarCierreCorteWindow
        // 4. Ejecuta cerrar.php
        // 5. Muestra CorteRealizadoWindow
        //
        // Solo continúa aquí cuando el backend cerró
        // realmente el turno.
        // ========================================================

        if (!realizado ||
            !ventana.CorteRealizadoCorrectamente)
        {
            return;
        }


        // ========================================================
        // EL BACKEND YA CERRÓ EL TURNO
        // ========================================================

        // Limpiar únicamente el turno en memoria.
        //
        // La caja, usuario, empresa y unidad operativa
        // permanecen seleccionados.
        PosSession
            .LimpiarTurno();


        // Limpiar únicamente el turno activo guardado
        // en terminal-config.json.
        //
        // NO elimina la configuración de la caja.
        await _terminalConfigurationService
            .LimpiarTurnoActivoAsync();


        // ========================================================
        // ACTUALIZAR INTERFAZ
        // ========================================================

        // Mostrar inmediatamente que ya no existe
        // un turno abierto.
        ViewModel
            .EstablecerSinTurno();


        // Volver a consultar el historial para que
        // aparezca el corte recién realizado.
        await ViewModel
            .CargarHistorialAsync();
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