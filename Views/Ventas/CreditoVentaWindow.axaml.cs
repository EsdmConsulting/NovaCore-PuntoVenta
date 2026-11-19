using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media;

using NovaCoreESDM.Models.Credito;


namespace NovaCoreESDM.Views.Ventas;


public partial class CreditoVentaWindow : Window
{
    // =========================================================
    // RESULTADO
    // =========================================================

    public bool CreditoConfirmado { get; private set; }


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public CreditoVentaWindow(
        string nombreCliente,
        string codigoCliente,
        string rfcCliente,
        decimal totalVenta,
        EstadoCreditoData credito)
    {
        InitializeComponent();


        // =====================================================
        // CLIENTE
        // =====================================================

        TextoCliente.Text =
            nombreCliente;


        TextoCodigoCliente.Text =
            string.IsNullOrWhiteSpace(codigoCliente)
                ? "Código: N/D"
                : $"Código: {codigoCliente}";


        TextoRfcCliente.Text =
            string.IsNullOrWhiteSpace(rfcCliente)
                ? "RFC: N/D"
                : $"RFC: {rfcCliente}";


        // =====================================================
        // TOTAL
        // =====================================================

        TextoTotalVenta.Text =
            $"${totalVenta:N2}";


        // =====================================================
        // CRÉDITO ACTUAL
        // =====================================================

        TextoLimiteCredito.Text =
            $"${credito.LimiteCredito:N2}";


        TextoSaldoDeudor.Text =
            $"${credito.SaldoDeudor:N2}";


        TextoDisponible.Text =
            $"${credito.Disponible:N2}";


        // =====================================================
        // PROYECCIÓN
        // =====================================================

        TextoSaldoProyectado.Text =
            $"${credito.SaldoProyectado:N2}";


        TextoDisponibleProyectado.Text =
            $"${credito.DisponibleProyectado:N2}";


        // =====================================================
        // CONDICIONES
        // =====================================================

        TextoDiasCredito.Text =
            credito.DiasCredito is int dias
                ? $"{dias} días"
                : "Sin definir";


        TextoTolerancia.Text =
            credito.ToleranciaDias is int tolerancia
                ? $"{tolerancia} días"
                : "Sin definir";


        TextoFechaInicio.Text =
            FormatearFecha(
                credito.FechaInicio
            );


        TextoFechaVencimiento.Text =
            FormatearFecha(
                credito.FechaVencimiento
            );


        TextoSemaforo.Text =
            string.IsNullOrWhiteSpace(
                credito.Semaforo)
                ? "N/D"
                : credito.Semaforo
                    .Trim()
                    .ToUpperInvariant();


        ConfigurarSemaforo(
            credito.Semaforo
        );


        // =====================================================
        // USO DE LA LÍNEA (solo visual)
        // =====================================================

        if (credito.LimiteCredito > 0)
        {
            var porcentaje =
                Math.Clamp(
                    credito.SaldoProyectado / credito.LimiteCredito * 100m,
                    0m,
                    100m);

            BarraUsoCredito.Value =
                (double)porcentaje;

            TextoUsoCredito.Text =
                $"Con esta venta usará el {porcentaje:0.#}% de su línea de crédito";
        }
        else
        {
            BarraUsoCredito.IsVisible =
                false;

            TextoUsoCredito.IsVisible =
                false;
        }


        // Por seguridad el foco arranca en "Cancelar": un Enter de
        // más nunca confirma la venta a crédito.
        Opened +=
            (_, _) =>
                Dispatcher.UIThread.Post(
                    () => BotonCancelar.Focus(),
                    DispatcherPriority.Input);
    }


    // =========================================================
    // TECLADO
    // =========================================================
    //
    // S     : confirma la venta a crédito (tecla deliberada).
    // Esc   : cancela (botón con IsCancel).
    // Enter : acciona el botón enfocado (por defecto, cancelar).
    // =========================================================

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        if (e.Key == Key.S)
        {
            e.Handled = true;

            Confirmar_Click(
                this,
                new RoutedEventArgs());

            return;
        }

        base.OnKeyDown(e);
    }


    // =========================================================
    // FORMATEAR FECHA
    // =========================================================

    private static string FormatearFecha(
        string? fecha)
    {
        if (string.IsNullOrWhiteSpace(
                fecha))
        {
            return "N/D";
        }


        if (
            DateTime.TryParse(
                fecha,
                out var fechaConvertida
            )
        )
        {
            return fechaConvertida
                .ToString(
                    "dd/MM/yyyy"
                );
        }


        return fecha;
    }


    // =========================================================
    // SEMÁFORO
    // =========================================================

    private void ConfigurarSemaforo(
        string? semaforo)
    {
        var valor =
            semaforo?
                .Trim()
                .ToUpperInvariant()
            ?? string.Empty;


        switch (valor)
        {
            case "VERDE":

                SemaforoBorder.Background =
                    new SolidColorBrush(
                        Color.Parse("#EAF9EE")
                    );

                TextoSemaforo.Foreground =
                    new SolidColorBrush(
                        Color.Parse("#16803B")
                    );

                break;


            case "AMARILLO":

                SemaforoBorder.Background =
                    new SolidColorBrush(
                        Color.Parse("#FFF7DB")
                    );

                TextoSemaforo.Foreground =
                    new SolidColorBrush(
                        Color.Parse("#A16207")
                    );

                break;


            case "ROJO":

                SemaforoBorder.Background =
                    new SolidColorBrush(
                        Color.Parse("#FDECEC")
                    );

                TextoSemaforo.Foreground =
                    new SolidColorBrush(
                        Color.Parse("#B42318")
                    );

                break;


            default:

                SemaforoBorder.Background =
                    new SolidColorBrush(
                        Color.Parse("#EEF2F6")
                    );

                TextoSemaforo.Foreground =
                    new SolidColorBrush(
                        Color.Parse("#64748B")
                    );

                break;
        }
    }


    // =========================================================
    // CONFIRMAR
    // =========================================================

    private void Confirmar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        CreditoConfirmado =
            true;


        Close(true);
    }


    // =========================================================
    // CANCELAR
    // =========================================================

    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        CreditoConfirmado =
            false;


        Close(false);
    }
}