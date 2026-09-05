using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
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