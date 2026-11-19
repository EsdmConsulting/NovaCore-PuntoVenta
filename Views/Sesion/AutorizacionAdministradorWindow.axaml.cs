using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NovaCoreESDM.Models.Cancelaciones;
using NovaCoreESDM.Services.Cancelaciones;

namespace NovaCoreESDM.Views.Sesion;

/// <summary>
/// Ventana genérica para pedir autorización de un administrador.
///
/// Uso:
///     var ventana = new AutorizacionAdministradorWindow(
///         "Título", "Descripción");
///
///     string? admin = await ventana.ShowDialog<string?>(ventanaPadre);
///
///     admin == null            -> canceló o no pasó la validación
///     admin == "Nombre Admin"  -> autorizado
///
/// IMPORTANTE: la autorización queda abierta en el servidor
/// ($_SESSION['cancelaciones_admin']). Quien llame a esta ventana
/// debe cerrarla con CancelacionesService.CerrarAutorizacionAsync()
/// cuando termine la operación protegida.
/// </summary>
public partial class AutorizacionAdministradorWindow : Window
{
    private readonly CancelacionesService _cancelacionesService = new();
    private ComboBox _administradorComboBox = null!;
    private TextBox _passwordTextBox = null!;
    private TextBlock _mensajeTextBlock = null!;
    private Button _autorizarButton = null!;
    private Button _cancelarButton = null!;
    private bool _cargando;

    public AutorizacionAdministradorWindow()
        : this(
            "Autorización de administrador",
            "Selecciona un administrador e ingresa su contraseña para continuar.")
    {
    }

    public AutorizacionAdministradorWindow(
        string titulo,
        string descripcion)
    {
        AvaloniaXamlLoader.Load(this);

        Title = titulo;

        this.FindControl<TextBlock>("TituloTextBlock")!.Text = titulo;
        this.FindControl<TextBlock>("DescripcionTextBlock")!.Text = descripcion;

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
            var respuesta =
                await _cancelacionesService.ObtenerListaAdministradoresAsync();

            if (respuesta.Res != 1 ||
                respuesta.Data is null ||
                respuesta.Data.Count == 0)
            {
                MostrarMensaje(
                    respuesta.Msg ??
                    "No fue posible cargar la lista de administradores.");

                return;
            }

            _administradorComboBox.ItemsSource = respuesta.Data;
            _administradorComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MostrarMensaje(
                $"No fue posible cargar los administradores: {ex.Message}");
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
            var respuesta =
                await _cancelacionesService.AutorizarAdministradorAsync(
                    usuario,
                    password);

            if (respuesta.Res != 1)
            {
                MostrarMensaje(
                    respuesta.Msg ?? "Las credenciales no son válidas.");

                _passwordTextBox.Text = string.Empty;
                return;
            }

            var nombre =
                respuesta.Data?.Nombre
                ?? administrador.Nombre
                ?? usuario;

            Close(nombre);
        }
        catch (Exception ex)
        {
            MostrarMensaje(
                $"No fue posible validar las credenciales: {ex.Message}");
        }
        finally
        {
            _cargando = false;
            ActualizarControles();
        }
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
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
        this.FindControl<TextBlock>("TextoAutorizar")!.Text =
            _cargando ? "Validando..." : "Validar y continuar";

        this.FindControl<PathIcon>("IconoAutorizar")!.IsVisible =
            !_cargando;

        this.FindControl<ProgressBar>("ProgresoAutorizar")!.IsVisible =
            _cargando;
    }
}
