using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media;

namespace NovaCoreESDM.Views.Cortes;

public partial class ConfirmarCierreCorteWindow
    : Window
{
    public ConfirmarCierreCorteWindow(
        decimal efectivoEsperado,
        decimal efectivoContado)
    {
        InitializeComponent();


        var diferencia =
            efectivoContado -
            efectivoEsperado;


        TxtEsperado.Text =
            FormatearMoneda(
                efectivoEsperado);


        TxtContado.Text =
            FormatearMoneda(
                efectivoContado);


        TxtDiferencia.Text =
            FormatearDiferencia(
                diferencia);


        // ========================================================
        // CUADRADO
        // ========================================================

        if (diferencia == 0m)
        {
            TxtEstado.Text =
                "CUADRADO";

            TxtEstado.Foreground =
                new SolidColorBrush(
                    Color.Parse("#15803D"));

            TxtDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#15803D"));
        }

        // ========================================================
        // SOBRANTE
        // ========================================================

        else if (diferencia > 0m)
        {
            TxtEstado.Text =
                "SOBRANTE";

            TxtEstado.Foreground =
                new SolidColorBrush(
                    Color.Parse("#B45309"));

            TxtDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#B45309"));
        }

        // ========================================================
        // FALTANTE
        // ========================================================

        else
        {
            TxtEstado.Text =
                "FALTANTE";

            TxtEstado.Foreground =
                new SolidColorBrush(
                    Color.Parse("#C61F2B"));

            TxtDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#C61F2B"));
        }


        // Por seguridad el foco arranca en "Volver": el Enter que abrió
        // esta ventana (o uno de más) nunca cierra el turno.
        Opened +=
            (_, _) =>
                Dispatcher.UIThread.Post(
                    () => BotonVolver.Focus(),
                    DispatcherPriority.Input);
    }


    // =========================================================
    // TECLADO
    // =========================================================
    //
    // S     : cierra el turno (tecla deliberada).
    // Esc   : vuelve (botón con IsCancel).
    // Enter : acciona el botón enfocado (por defecto, volver).
    // =========================================================

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        if (e.Key == Key.S)
        {
            e.Handled = true;

            Close(true);

            return;
        }

        base.OnKeyDown(e);
    }


    private void Volver_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }


    private void Confirmar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }


    private static string FormatearMoneda(
        decimal valor)
    {
        return
            $"${valor:N2}";
    }


    private static string FormatearDiferencia(
        decimal valor)
    {
        if (valor > 0m)
        {
            return
                $"+${valor:N2}";
        }


        if (valor < 0m)
        {
            return
                $"-${Math.Abs(valor):N2}";
        }


        return
            "$0.00";
    }
}