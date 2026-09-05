using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using NovaCoreESDM.ViewModels.Cortes;

namespace NovaCoreESDM.Views.Cortes;

public partial class CortesView
    : UserControl
{
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


    private void ViewModel_RealizarCorteSolicitado(
        object? sender,
        EventArgs e)
    {
        /*
         * SIGUIENTE PASO:
         *
         * Aquí abriremos RealizarCorteWindow.
         *
         * El ViewModel ya tendrá cargados:
         *
         * - Fondo inicial
         * - Ventas en efectivo
         * - Abonos de crédito
         * - Ingresos
         * - Retiros
         * - Egresos
         * - Devoluciones
         * - Efectivo esperado
         *
         * Entonces la ventana solamente pedirá
         * el efectivo contado físicamente.
         */
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


        /*
         * En el siguiente paso también agregaremos aquí:
         *
         * await ViewModel.CargarHistorialAsync();
         *
         * cuando creemos CortesService.
         */
    }
}