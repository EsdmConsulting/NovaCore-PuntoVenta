using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NovaCoreESDM.Models.Turno;

namespace NovaCoreESDM.Views.Turno;

public partial class ContinuarTurnoWindow : Window
{
    private readonly TurnoAbierto _turno;
    private readonly bool _mismoUsuario;

    private TextBlock _turnoTextBlock = null!;
    private TextBlock _cajaTextBlock = null!;
    private TextBlock _usuarioTextBlock = null!;
    private TextBlock _fechaTextBlock = null!;
    private TextBlock _fondoTextBlock = null!;
    private TextBlock _tituloAdvertenciaTextBlock = null!;
    private TextBlock _mensajeAdvertenciaTextBlock = null!;

    public ContinuarTurnoWindow(TurnoAbierto turno, bool mismoUsuario)
    {
        ArgumentNullException.ThrowIfNull(turno);

        AvaloniaXamlLoader.Load(this);

        // Resolver controles explícitamente después de cargar el XAML.
        // Así, si falta un x:Name, se informa cuál es en vez de lanzar
        // una NullReferenceException al asignar su propiedad Text.
        _turnoTextBlock = ObtenerControl("TurnoTextBlock");
        _cajaTextBlock = ObtenerControl("CajaTextBlock");
        _usuarioTextBlock = ObtenerControl("UsuarioTextBlock");
        _fechaTextBlock = ObtenerControl("FechaTextBlock");
        _fondoTextBlock = ObtenerControl("FondoTextBlock");
        _tituloAdvertenciaTextBlock = ObtenerControl("TituloAdvertenciaTextBlock");
        _mensajeAdvertenciaTextBlock = ObtenerControl("MensajeAdvertenciaTextBlock");

        _turno = turno;
        _mismoUsuario = mismoUsuario;

        CargarInformacion();
    }

    private TextBlock ObtenerControl(string nombre)
    {
        return this.FindControl<TextBlock>(nombre)
            ?? throw new InvalidOperationException(
                $"No se encontró el control '{nombre}' en ContinuarTurnoWindow.axaml. " +
                "Verifica su x:Name y que el XAML se haya cargado correctamente.");
    }

    private void CargarInformacion()
    {
        _turnoTextBlock.Text = $"#{_turno.IdTurno}";

        _cajaTextBlock.Text =
            !string.IsNullOrWhiteSpace(_turno.CajaNombre)
                ? _turno.CajaNombre
                : _turno.CajaCodigo ?? "Caja no disponible";

        _usuarioTextBlock.Text = ObtenerNombreUsuario();
        _fechaTextBlock.Text = FormatearFecha(_turno.FechaApertura);
        _fondoTextBlock.Text = _turno.FondoInicial.ToString("C2");

        if (_mismoUsuario)
        {
            _tituloAdvertenciaTextBlock.Text = "Ya tienes un turno abierto";
            _mensajeAdvertenciaTextBlock.Text =
                "Esta caja ya tiene un turno abierto por tu usuario. " +
                "Puedes continuar trabajando sobre el mismo turno.";
        }
        else
        {
            _tituloAdvertenciaTextBlock.Text = "Turno abierto por otro usuario";
            _mensajeAdvertenciaTextBlock.Text =
                "Esta caja fue abierta por otro usuario. " +
                "Si continúas, las nuevas operaciones formarán parte " +
                "del mismo turno y del mismo corte.";
        }
    }

    private string ObtenerNombreUsuario()
    {
        if (!string.IsNullOrWhiteSpace(_turno.UsuarioAperturaNombre))
            return _turno.UsuarioAperturaNombre;

        if (!string.IsNullOrWhiteSpace(_turno.UsuarioApertura))
            return _turno.UsuarioApertura;

        return "Usuario no disponible";
    }

    private static string FormatearFecha(string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha))
            return "—";

        if (DateTimeOffset.TryParse(fecha, out var fechaConvertida))
        {
            return fechaConvertida
                .ToLocalTime()
                .ToString("dd/MM/yyyy HH:mm");
        }

        return fecha;
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Continuar_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
