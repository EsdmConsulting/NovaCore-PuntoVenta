using System;
using System.Globalization;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

using NovaCoreESDM.Models.Turno;
using NovaCoreESDM.Services.Turnos;

namespace NovaCoreESDM.Views.Cortes;

public partial class RealizarCorteWindow : Window
{
    private readonly TurnosService
        _turnosService;

    private readonly ResumenTurnoData
        _resumen;

    private decimal
        _efectivoEsperado;

    private decimal
        _efectivoContado;

    private bool
        _tieneEfectivoCapturado;

    private bool
        _procesando;


    // ============================================================
    // RESULTADO
    // ============================================================

    public bool CorteRealizadoCorrectamente
    {
        get;
        private set;
    }


    public CerrarTurnoResponse? ResultadoCierre
    {
        get;
        private set;
    }


    // ============================================================
    // ACCIÓN DESPUÉS DEL CORTE
    // ============================================================

    public AccionDespuesCorte AccionPosterior
    {
        get;
        private set;
    } = AccionDespuesCorte.Ninguna;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public RealizarCorteWindow(
        ResumenTurnoData resumen)
    {
        InitializeComponent();

        _turnosService =
            new TurnosService();

        _resumen =
            resumen;

        CargarResumen();
    }


    // ============================================================
    // CARGAR INFORMACIÓN
    // ============================================================

    private void CargarResumen()
    {
        var turno =
            _resumen.Turno;

        var ventas =
            _resumen.Ventas;

        var abonos =
            _resumen.AbonosCredito;

        var caja =
            _resumen.Caja;


        if (turno is null ||
            caja is null)
        {
            MostrarError(
                "No fue posible obtener la información del turno.");

            BtnConfirmar.IsEnabled =
                false;

            return;
        }


        _efectivoEsperado =
            caja.EfectivoEsperado;


        // ========================================================
        // INFORMACIÓN DEL TURNO
        // ========================================================

        var nombreCaja =
            !string.IsNullOrWhiteSpace(
                turno.CajaNombre)
                ? turno.CajaNombre
                : "Caja";


        TxtInformacionTurno.Text =
            $"{nombreCaja} · Turno #{turno.Id}";


        // ========================================================
        // EFECTIVO
        // ========================================================

        var abonosEfectivo =
            abonos?.Efectivo ?? 0m;


        /*
         * IMPORTANTE:
         *
         * caja.Ingresos incluye los abonos de crédito
         * realizados en efectivo.
         *
         * Como visualmente los mostramos por separado,
         * "Otros ingresos" debe excluir esos abonos.
         */

        var otrosIngresos =
            Math.Max(
                0m,
                caja.Ingresos -
                abonosEfectivo);


        TxtFondoInicial.Text =
            FormatearMoneda(
                caja.FondoInicial);


        TxtVentasEfectivo.Text =
            FormatearMoneda(
                ventas?.Efectivo ?? 0m);


        TxtAbonosCreditoEfectivo.Text =
            FormatearMoneda(
                abonosEfectivo);


        TxtOtrosIngresos.Text =
            FormatearMoneda(
                otrosIngresos);


        TxtRetiros.Text =
            FormatearSalida(
                caja.Retiros);


        TxtEgresos.Text =
            FormatearSalida(
                caja.Egresos);


        TxtDevoluciones.Text =
            FormatearSalida(
                caja.Devoluciones);


        TxtEfectivoEsperado.Text =
            FormatearMoneda(
                _efectivoEsperado);


        ActualizarDiferencia();
    }


    // ============================================================
    // EFECTIVO CONTADO
    // ============================================================

    private void EfectivoContado_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        var texto =
            TxtEfectivoContado.Text?
                .Trim();


        if (string.IsNullOrWhiteSpace(
                texto))
        {
            _tieneEfectivoCapturado =
                false;

            _efectivoContado =
                0m;

            ActualizarDiferencia();

            return;
        }


        if (TryParseDecimal(
                texto,
                out var valor) &&
            valor >= 0m)
        {
            _efectivoContado =
                valor;

            _tieneEfectivoCapturado =
                true;

            OcultarError();
        }
        else
        {
            _efectivoContado =
                0m;

            _tieneEfectivoCapturado =
                false;
        }


        ActualizarDiferencia();
    }


    // ============================================================
    // ACTUALIZAR DIFERENCIA
    // ============================================================

    private void ActualizarDiferencia()
    {
        if (!_tieneEfectivoCapturado)
        {
            TxtDiferencia.Text =
                "$0.00";

            TxtEstadoDiferencia.Text =
                "PENDIENTE";

            TxtEstadoDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#64748B"));

            TxtDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#334155"));

            BorderDiferencia.Background =
                new SolidColorBrush(
                    Color.Parse("#F8FAFC"));

            BorderDiferencia.BorderBrush =
                new SolidColorBrush(
                    Color.Parse("#CBD5E1"));

            BtnConfirmar.IsEnabled =
                false;

            return;
        }


        var diferencia =
            _efectivoContado -
            _efectivoEsperado;


        TxtDiferencia.Text =
            FormatearDiferencia(
                diferencia);


        // ========================================================
        // CUADRADO
        // ========================================================

        if (diferencia == 0m)
        {
            TxtEstadoDiferencia.Text =
                "CUADRADO";

            TxtEstadoDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#15803D"));

            TxtDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#15803D"));

            BorderDiferencia.Background =
                new SolidColorBrush(
                    Color.Parse("#F0FDF4"));

            BorderDiferencia.BorderBrush =
                new SolidColorBrush(
                    Color.Parse("#BBF7D0"));
        }

        // ========================================================
        // SOBRANTE
        // ========================================================

        else if (diferencia > 0m)
        {
            TxtEstadoDiferencia.Text =
                "SOBRANTE";

            TxtEstadoDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#B45309"));

            TxtDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#B45309"));

            BorderDiferencia.Background =
                new SolidColorBrush(
                    Color.Parse("#FFFBEB"));

            BorderDiferencia.BorderBrush =
                new SolidColorBrush(
                    Color.Parse("#FDE68A"));
        }

        // ========================================================
        // FALTANTE
        // ========================================================

        else
        {
            TxtEstadoDiferencia.Text =
                "FALTANTE";

            TxtEstadoDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#C61F2B"));

            TxtDiferencia.Foreground =
                new SolidColorBrush(
                    Color.Parse("#C61F2B"));

            BorderDiferencia.Background =
                new SolidColorBrush(
                    Color.Parse("#FEF2F2"));

            BorderDiferencia.BorderBrush =
                new SolidColorBrush(
                    Color.Parse("#FECACA"));
        }


        BtnConfirmar.IsEnabled =
            !_procesando;
    }


    // ============================================================
    // CONFIRMAR
    // ============================================================

    private async void Confirmar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_procesando)
        {
            return;
        }


        if (!_tieneEfectivoCapturado)
        {
            MostrarError(
                "Ingresa el efectivo contado físicamente.");

            return;
        }


        var turno =
            _resumen.Turno;


        if (turno is null ||
            turno.Id <= 0)
        {
            MostrarError(
                "No se encontró un turno válido.");

            return;
        }


        var confirmar =
            new ConfirmarCierreCorteWindow(
                _efectivoEsperado,
                _efectivoContado);


        var confirmado =
            await confirmar
                .ShowDialog<bool>(
                    this);


        if (!confirmado)
        {
            return;
        }


        await EjecutarCierreAsync(
            turno.Id);
    }


    // ============================================================
    // EJECUTAR CIERRE
    // ============================================================

    private async Task EjecutarCierreAsync(
        int idTurno)
    {
        try
        {
            _procesando =
                true;

            BtnConfirmar.IsEnabled =
                false;

            BtnCancelar.IsEnabled =
                false;

            TxtEfectivoContado.IsEnabled =
                false;

            TxtObservaciones.IsEnabled =
                false;

            OcultarError();


            var request =
                new CerrarTurnoRequest
                {
                    IdTurno =
                        idTurno,

                    EfectivoDeclarado =
                        _efectivoContado,

                    Observaciones =
                        string.IsNullOrWhiteSpace(
                            TxtObservaciones.Text)
                            ? null
                            : TxtObservaciones.Text.Trim()
                };


            var resultado =
                await _turnosService
                    .CerrarTurnoAsync(
                        request);


            if (resultado.Res != 1)
            {
                MostrarError(
                    resultado.Msg ??
                    "No fue posible realizar el corte.");

                return;
            }


            CorteRealizadoCorrectamente =
                true;

            ResultadoCierre =
                resultado;


            // ========================================================
            // MOSTRAR RESULTADO FINAL
            // ========================================================

            var ventanaResultado =
                new CorteRealizadoWindow(
                    resultado);


            var accion =
                await ventanaResultado
                    .ShowDialog<AccionDespuesCorte>(
                        this);


            // ========================================================
            // GUARDAR ACCIÓN ELEGIDA
            // ========================================================

            AccionPosterior =
                accion;


            // ========================================================
            // CERRAR VENTANA DE CORTE
            // ========================================================

            Close(
                true);
        }
        catch (Exception ex)
        {
            MostrarError(
                $"No fue posible realizar el corte: {ex.Message}");
        }
        finally
        {
            _procesando =
                false;


            if (!CorteRealizadoCorrectamente)
            {
                BtnCancelar.IsEnabled =
                    true;

                TxtEfectivoContado.IsEnabled =
                    true;

                TxtObservaciones.IsEnabled =
                    true;

                ActualizarDiferencia();
            }
        }
    }


    // ============================================================
    // CANCELAR
    // ============================================================

    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (_procesando)
        {
            return;
        }


        Close(
            false);
    }


    // ============================================================
    // MENSAJES
    // ============================================================

    private void MostrarError(
        string mensaje)
    {
        TxtMensaje.Text =
            mensaje;

        TxtMensaje.IsVisible =
            true;
    }


    private void OcultarError()
    {
        TxtMensaje.Text =
            string.Empty;

        TxtMensaje.IsVisible =
            false;
    }


    // ============================================================
    // PARSE DECIMAL
    // ============================================================

    private static bool TryParseDecimal(
        string texto,
        out decimal valor)
    {
        /*
         * Primero cultura actual:
         *
         * 1,250.50 / 1250.50 dependiendo
         * de la configuración del equipo.
         */

        if (decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out valor))
        {
            return true;
        }


        /*
         * Fallback invariant:
         *
         * 1250.50
         */

        if (decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out valor))
        {
            return true;
        }


        valor =
            0m;

        return false;
    }


    // ============================================================
    // FORMATO MONEDA
    // ============================================================

    private static string FormatearMoneda(
        decimal valor)
    {
        return
            $"${valor:N2}";
    }


    private static string FormatearSalida(
        decimal valor)
    {
        return
            $"-${Math.Abs(valor):N2}";
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