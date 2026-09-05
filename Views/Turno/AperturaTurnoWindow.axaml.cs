using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using NovaCoreESDM.Models.Turno;
using NovaCoreESDM.ViewModels;
using NovaCoreESDM.ViewModels.Turno;

namespace NovaCoreESDM.Views.Turno;

public partial class AperturaTurnoWindow : Window
{
    private readonly AperturaTurnoViewModel
        _viewModel;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public AperturaTurnoWindow()
    {
        AvaloniaXamlLoader.Load(this);


        _viewModel =
            new AperturaTurnoViewModel();


        DataContext =
            _viewModel;


        // =====================================================
        // EVENTOS
        // =====================================================

        _viewModel.SolicitarConfirmacionTurno +=
            OnSolicitarConfirmacionTurno;


        _viewModel.SolicitarConfirmacionTurnoExistente +=
            OnSolicitarConfirmacionTurnoExistente;


        _viewModel.TurnoAbiertoCorrectamente +=
            OnTurnoAbiertoCorrectamente;
    }


    // =========================================================
    // CONFIRMAR APERTURA NUEVA
    // =========================================================

    private async void OnSolicitarConfirmacionTurno(
        decimal fondoInicial)
    {
        var confirmacion =
            new ConfirmarAperturaWindow(
                fondoInicial);


        var resultado =
            await confirmacion
                .ShowDialog<bool>(this);


        if (!resultado)
        {
            return;
        }


        await _viewModel
            .ConfirmarAperturaAsync(
                fondoInicial);
    }


    // =========================================================
    // TURNO YA EXISTENTE
    // =========================================================

    private async void OnSolicitarConfirmacionTurnoExistente(
        TurnoAbierto turno,
        bool mismoUsuario)
    {
        var confirmacion =
            new ContinuarTurnoWindow(
                turno,
                mismoUsuario);


        var resultado =
            await confirmacion
                .ShowDialog<bool>(this);


        if (!resultado)
        {
            return;
        }


        await _viewModel
            .ContinuarTurnoExistenteAsync(
                turno);
    }


    // =========================================================
    // TURNO LISTO
    // =========================================================

    private void OnTurnoAbiertoCorrectamente()
    {
        var mainWindow =
            new MainWindow
            {
                DataContext =
                    new MainViewModel()
            };


        mainWindow.Show();


        Close();
    }


    // =========================================================
    // LIMPIEZA
    // =========================================================

    protected override void OnClosed(
        EventArgs e)
    {
        _viewModel.SolicitarConfirmacionTurno -=
            OnSolicitarConfirmacionTurno;


        _viewModel.SolicitarConfirmacionTurnoExistente -=
            OnSolicitarConfirmacionTurnoExistente;


        _viewModel.TurnoAbiertoCorrectamente -=
            OnTurnoAbiertoCorrectamente;


        base.OnClosed(e);
    }
}