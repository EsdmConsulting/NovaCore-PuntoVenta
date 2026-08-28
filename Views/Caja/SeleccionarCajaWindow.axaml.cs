using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NovaCoreESDM.Models.Configuration;
using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.ViewModels.Caja;
using NovaCoreESDM.Views.Turno;

using CajaModel = NovaCoreESDM.Models.Caja.Caja;

namespace NovaCoreESDM.Views.Caja;

public partial class SeleccionarCajaWindow : Window
{
    private readonly SeleccionarCajaViewModel _viewModel;

    private readonly TerminalConfigurationService
        _terminalConfigurationService;

    public SeleccionarCajaWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _viewModel =
            new SeleccionarCajaViewModel();

        _terminalConfigurationService =
            new TerminalConfigurationService();

        DataContext =
            _viewModel;

        _viewModel.CajaConfirmada +=
            OnCajaConfirmada;

        Opened +=
            OnWindowOpened;
    }

    private async void OnWindowOpened(
        object? sender,
        EventArgs e)
    {
        await _viewModel.CargarCajasAsync();
    }

    private async void OnCajaConfirmada(
        CajaModel caja)
    {
        var configuracion =
            new TerminalConfiguration
            {
                IdCaja = caja.Id,
                UuidCaja = caja.Uuid,

                IdEmpresa =
                    caja.IdEmpresa,

                IdUnidadOperativa =
                    caja.IdUnidadOperativa,

                UnidadCodigo =
                    caja.UnidadCodigo,

                UnidadNombre =
                    caja.UnidadNombre,

                CodigoCaja =
                    caja.Codigo,

                NombreCaja =
                    caja.Nombre,

                NumeroTerminal =
                    caja.NumeroTerminal,

                SerieTicket =
                    caja.SerieTicket
            };

        await _terminalConfigurationService
            .GuardarConfiguracionAsync(
                configuracion
            );

        var aperturaTurnoWindow =
            new AperturaTurnoWindow();

        aperturaTurnoWindow.Show();

        Close();
    }

    protected override void OnClosed(
        EventArgs e)
    {
        _viewModel.CajaConfirmada -=
            OnCajaConfirmada;

        Opened -=
            OnWindowOpened;

        base.OnClosed(e);
    }
}