using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NovaCoreESDM.Models.Cancelaciones;
using NovaCoreESDM.Services.Cancelaciones;

namespace NovaCoreESDM.Views.Caja;

public partial class CambiarCajaAutorizacionWindow : Window
{
    private readonly CancelacionesService _cancelacionesService = new();
    private ComboBox _administradorComboBox = null!;
    private TextBox _passwordTextBox = null!;
    private TextBlock _mensajeTextBlock = null!;
    private Button _autorizarButton = null!;
    private Button _cancelarButton = null!;
    private bool _cargando;

    public CambiarCajaAutorizacionWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _administradorComboBox = this.FindControl<ComboBox>("AdministradorComboBox")!;
        _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox")!;
        _mensajeTextBlock = this.FindControl<TextBlock>("MensajeTextBlock")!;
        _autorizarButton = this.FindControl<Button>("AutorizarButton")!;
        _cancelarButton = this.FindControl<Button>("CancelarButton")!;

        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        _cargando = true;
        ActualizarControles();
        try
        {
            var respuesta = await _cancelacionesService.ObtenerListaAdministradoresAsync();
            if (respuesta.Res != 1 || respuesta.Data is null || respuesta.Data.Count == 0)
            {
                MostrarMensaje(respuesta.Msg ?? "No fue posible cargar la lista de administradores.");
                return;
            }

            _administradorComboBox.ItemsSource = respuesta.Data;
            _administradorComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MostrarMensaje($"No fue posible cargar los administradores: {ex.Message}");
        }
        finally
        {
            _cargando = false;
            ActualizarControles();

            // Listo para escribir la contraseña y presionar Enter.
            _passwordTextBox.Focus();
        }
    }

    private async void Autorizar_Click(object? sender, RoutedEventArgs e)
    {
        OcultarMensaje();

        if (_administradorComboBox.SelectedItem is not AdministradorItem administrador)
        {
            MostrarMensaje("Selecciona un administrador.");
            return;
        }

        var usuario = administrador.Usuario;
        var password = _passwordTextBox.Text;

        if (string.IsNullOrWhiteSpace(usuario))
        {
            MostrarMensaje("El administrador seleccionado no tiene un usuario válido.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            MostrarMensaje("Ingresa la contraseña del administrador.");
            return;
        }

        _cargando = true;
        ActualizarControles();
        try
        {
            var respuesta = await _cancelacionesService.AutorizarAdministradorAsync(
                usuario,
                password);

            if (respuesta.Res != 1)
            {
                MostrarMensaje(respuesta.Msg ?? "Las credenciales no son válidas.");
                _passwordTextBox.Text = string.Empty;
                return;
            }

            Close(true);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"No fue posible validar las credenciales: {ex.Message}");
        }
        finally
        {
            _cargando = false;
            ActualizarControles();
        }
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void MostrarMensaje(string mensaje)
    {
        _mensajeTextBlock.Text = mensaje;
        _mensajeTextBlock.IsVisible = true;
    }

    private void OcultarMensaje()
    {
        _mensajeTextBlock.Text = string.Empty;
        _mensajeTextBlock.IsVisible = false;
    }

    private void ActualizarControles()
    {
        _administradorComboBox.IsEnabled = !_cargando;
        _passwordTextBox.IsEnabled = !_cargando;
        _autorizarButton.IsEnabled = !_cargando;
        _cancelarButton.IsEnabled = !_cargando;
        // El botón tiene ícono + texto; se cambia solo el texto y se
        // muestra una barra de carga mientras valida.
        this.FindControl<TextBlock>("TextoAutorizar")!.Text = _cargando ? "Validando..." : "Validar y continuar";
        this.FindControl<PathIcon>("IconoAutorizar")!.IsVisible = !_cargando;
        this.FindControl<ProgressBar>("ProgresoAutorizar")!.IsVisible = _cargando;
    }
}
