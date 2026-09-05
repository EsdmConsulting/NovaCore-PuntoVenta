using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

using NovaCoreESDM.Models.Turno;

namespace NovaCoreESDM.Views.Cortes;

public partial class CorteRealizadoWindow
    : Window
{
    public CorteRealizadoWindow(
        CerrarTurnoResponse resultado)
    {
        InitializeComponent();


        var data =
            resultado.Data;


        var resumen =
            data?.Resumen;


        if (data is not null)
        {
            TxtCorte.Text =
                data.IdCorte > 0
                    ? $"Corte #{data.IdCorte}"
                    : "Corte registrado";
        }


        if (resumen is null)
        {
            return;
        }


        TxtEsperado.Text =
            FormatearMoneda(
                resumen.EfectivoSistema);


        TxtDeclarado.Text =
            FormatearMoneda(
                resumen.EfectivoDeclarado);


        TxtDiferencia.Text =
            FormatearDiferencia(
                resumen.Diferencia);


        ConfigurarEstado(
            resumen.Diferencia);
    }


    private void ConfigurarEstado(
        decimal diferencia)
    {
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

            BorderEstado.Background =
                new SolidColorBrush(
                    Color.Parse("#EAF7EF"));

            return;
        }


        // ========================================================
        // SOBRANTE
        // ========================================================

        if (diferencia > 0m)
        {
            TxtEstado.Text =
                "SOBRANTE";

            TxtEstado.Foreground =
                new SolidColorBrush(
                    Color.Parse("#B45309"));

            TxtDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#B45309"));

            BorderEstado.Background =
                new SolidColorBrush(
                    Color.Parse("#FFF8E8"));

            return;
        }


        // ========================================================
        // FALTANTE
        // ========================================================

        TxtEstado.Text =
            "FALTANTE";

        TxtEstado.Foreground =
            new SolidColorBrush(
                Color.Parse("#C61F2B"));

        TxtDiferencia.Foreground =
            new SolidColorBrush(
                Color.Parse("#C61F2B"));

        BorderEstado.Background =
            new SolidColorBrush(
                Color.Parse("#FEF2F2"));
    }


    private void Finalizar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
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