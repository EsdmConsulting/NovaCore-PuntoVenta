using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NovaCoreESDM.ViewModels;
using NovaCoreESDM.ViewModels.Turno;

namespace NovaCoreESDM.Views.Turno;

public partial class AperturaTurnoWindow : Window
{
    private readonly AperturaTurnoViewModel _viewModel;

    public AperturaTurnoWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _viewModel =
            new AperturaTurnoViewModel();

        DataContext =
            _viewModel;

        _viewModel.SolicitarConfirmacionTurno +=
            OnSolicitarConfirmacionTurno;

        _viewModel.TurnoAbiertoCorrectamente +=
            OnTurnoAbiertoCorrectamente;
    }

    private async void OnSolicitarConfirmacionTurno(
        decimal fondoInicial)
    {
        var confirmacion =
            new ConfirmarAperturaWindow(
                fondoInicial
            );

        var resultado =
            await confirmacion
                .ShowDialog<bool>(this);

        if (!resultado)
            return;

        await _viewModel
            .ConfirmarAperturaAsync(
                fondoInicial
            );
    }

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

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.SolicitarConfirmacionTurno -=
            OnSolicitarConfirmacionTurno;

        _viewModel.TurnoAbiertoCorrectamente -=
            OnTurnoAbiertoCorrectamente;

        base.OnClosed(e);
    }
}