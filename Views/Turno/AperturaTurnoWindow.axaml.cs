using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
        try
        {
            if (turno is null)
            {
                await MostrarErrorAsync(
                    "El servidor indicó que existe un turno abierto, " +
                    "pero no devolvió los datos del turno.");

                return;
            }

            var confirmacion = new ContinuarTurnoWindow(
                turno,
                mismoUsuario);

            var resultado = await confirmacion.ShowDialog<bool>(this);

            if (!resultado)
                return;

            await _viewModel.ContinuarTurnoExistenteAsync(turno);

            // Si el ViewModel no pudo continuar, la ventana permanece abierta
            // y muestra su MensajeError enlazado en XAML.
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                "ERROR MOSTRANDO O CONFIRMANDO TURNO EXISTENTE:");
            Console.Error.WriteLine(ex);

            try
            {
                await MostrarErrorAsync(
                    "No fue posible mostrar o continuar el turno existente.\n\n" +
                    ex.Message);
            }
            catch (Exception dialogEx)
            {
                // La excepción secundaria tampoco debe escapar de async void.
                Console.Error.WriteLine(
                    "ERROR MOSTRANDO EL MENSAJE DE ERROR:");
                Console.Error.WriteLine(dialogEx);
            }
        }
    }

    private async Task MostrarErrorAsync(string mensaje)
    {
        var dialogo = new Window
        {
            Title = "No fue posible continuar",
            Width = 460,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(22),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = mensaje,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new Button
                    {
                        Content = "Aceptar",
                        HorizontalAlignment =
                            Avalonia.Layout.HorizontalAlignment.Right,
                        MinWidth = 100
                    }
                }
            }
        };

        if (dialogo.Content is StackPanel panel &&
            panel.Children[1] is Button boton)
        {
            boton.Click += (_, _) => dialogo.Close();
        }

        await dialogo.ShowDialog(this);
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


        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainWindow;
        }

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