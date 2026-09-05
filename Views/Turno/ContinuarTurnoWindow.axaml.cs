using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using NovaCoreESDM.Models.Turno;

namespace NovaCoreESDM.Views.Turno;

public partial class ContinuarTurnoWindow : Window
{
    private readonly TurnoAbierto
        _turno;

    private readonly bool
        _mismoUsuario;


    public ContinuarTurnoWindow(
        TurnoAbierto turno,
        bool mismoUsuario)
    {
        AvaloniaXamlLoader.Load(this);


        _turno =
            turno;

        _mismoUsuario =
            mismoUsuario;


        CargarInformacion();
    }


    // =========================================================
    // CARGAR INFORMACIÓN
    // =========================================================

    private void CargarInformacion()
    {
        TurnoTextBlock.Text =
            $"#{_turno.IdTurno}";


        CajaTextBlock.Text =
            string.IsNullOrWhiteSpace(
                _turno.CajaNombre)

                ? _turno.CajaCodigo

                : _turno.CajaNombre;


        UsuarioTextBlock.Text =
            ObtenerNombreUsuario();


        FechaTextBlock.Text =
            FormatearFecha(
                _turno.FechaApertura);


        FondoTextBlock.Text =
            _turno.FondoInicial
                .ToString("C2");


        // =====================================================
        // MENSAJE SEGÚN USUARIO
        // =====================================================

        if (_mismoUsuario)
        {
            TituloAdvertenciaTextBlock.Text =
                "Ya tienes un turno abierto";


            MensajeAdvertenciaTextBlock.Text =
                "Esta caja ya tiene un turno abierto por tu usuario. " +
                "Puedes continuar trabajando sobre el mismo turno.";
        }
        else
        {
            TituloAdvertenciaTextBlock.Text =
                "Turno abierto por otro usuario";


            MensajeAdvertenciaTextBlock.Text =
                "Esta caja fue abierta por otro usuario. " +
                "Si continúas, las nuevas operaciones formarán parte " +
                "del mismo turno y del mismo corte.";
        }
    }


    // =========================================================
    // NOMBRE DE USUARIO
    // =========================================================

    private string ObtenerNombreUsuario()
    {
        if (!string.IsNullOrWhiteSpace(
                _turno.UsuarioAperturaNombre))
        {
            return _turno.UsuarioAperturaNombre;
        }


        if (!string.IsNullOrWhiteSpace(
                _turno.UsuarioApertura))
        {
            return _turno.UsuarioApertura;
        }


        return "Usuario no disponible";
    }


    // =========================================================
    // FORMATEAR FECHA
    // =========================================================

    private static string FormatearFecha(
        string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha))
        {
            return "—";
        }


        if (DateTimeOffset.TryParse(
                fecha,
                out var fechaConvertida))
        {
            return fechaConvertida
                .ToLocalTime()
                .ToString(
                    "dd/MM/yyyy HH:mm");
        }


        return fecha;
    }


    // =========================================================
    // CANCELAR
    // =========================================================

    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }


    // =========================================================
    // CONTINUAR
    // =========================================================

    private void Continuar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }
}