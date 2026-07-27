using System;
using Avalonia.Controls;
using NovaCoreESDM.ViewModels;

namespace NovaCoreESDM.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow()
    {
        InitializeComponent();

        _viewModel = new LoginViewModel();
        DataContext = _viewModel;

        _viewModel.InicioSesionExitoso += AbrirVentanaPrincipal;
    }

    private void AbrirVentanaPrincipal()
    {
        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel()
        };

        mainWindow.Show();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.InicioSesionExitoso -= AbrirVentanaPrincipal;
        base.OnClosed(e);
    }
}