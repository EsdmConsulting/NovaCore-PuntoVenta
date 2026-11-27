using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NovaCoreESDM.Models.Configuration;
using NovaCoreESDM.Models.TerminalPago;
using NovaCoreESDM.Models.Ventas;
using NovaCoreESDM.Services.TerminalPago;

namespace NovaCoreESDM.Views.Ventas;

/// <summary>
/// Cobro con tarjeta usando una terminal vinculada a este equipo.
///
/// Dos formas de cobrar:
///
///  1. MANUAL: la terminal no está conectada al sistema. El cajero cobra
///     en la terminal y captura las terminaciones del número de pago.
///
///  2. DIRECTO (solo inalámbricas): el POS envía el importe a la pantalla
///     de la terminal, espera el resultado y lo valida automáticamente.
///
/// IMPORTANTE: esta ventana NO registra el pago en el ERP ni finaliza la
/// venta. Solo deja el resultado en <see cref="Resultado"/>; VentasView
/// registra el pago únicamente si quedó aprobado.
/// </summary>
public partial class PagoTarjetaTerminalWindow : Window
{
    private const int IntentosSinRespuestaParaCerrar = 5;

    private readonly List<TerminalPagoConfiguration> _terminales;
    private readonly TerminalPagoClient _client = new();
    private readonly string _claveVenta = string.Empty;
    private readonly string _folio;
    private readonly decimal _importe;

    private bool _modoDirecto;
    private bool _procesando;
    private bool _cancelando;
    private bool _permitirCierre;


    /// <summary>null = el cajero salió sin cobrar.</summary>
    public ResultadoCobroTarjeta? Resultado { get; private set; }


    public PagoTarjetaTerminalWindow()
    {
        InitializeComponent();

        _terminales = new();
        _folio = string.Empty;
    }

    public PagoTarjetaTerminalWindow(
        decimal importe,
        string claveVenta,
        string folio,
        IEnumerable<TerminalPagoConfiguration> terminales)
        : this()
    {
        _importe = importe;
        _claveVenta = claveVenta;
        _folio = folio;
        _terminales = terminales.ToList();

        TextoImporte.Text = $"${importe:N2}";

        ComboTerminal.ItemsSource = _terminales;
        ComboTerminal.SelectedIndex = 0;

        Closing += (_, e) =>
        {
            // Con un cobro en curso no se puede cerrar con la X:
            // la operación seguiría viva en la terminal.
            if (_procesando && !_permitirCierre)
                e.Cancel = true;
        };
    }


    // =========================================================
    // SELECCIÓN
    // =========================================================

    private TerminalPagoConfiguration? TerminalActual =>
        ComboTerminal.SelectedItem as TerminalPagoConfiguration;


    private void Terminal_Changed(object? sender, SelectionChangedEventArgs e)
    {
        var terminal = TerminalActual;

        if (terminal is null)
            return;

        TextoDetalleTerminal.Text = terminal.Resumen;

        // Las alámbricas solo admiten captura manual.
        BotonModoDirecto.IsEnabled = terminal.EsInalambrica;

        _modoDirecto = terminal.EsInalambrica;

        ActualizarModo();
    }

    private void ModoDirecto_Click(object? sender, RoutedEventArgs e)
    {
        if (TerminalActual?.EsInalambrica != true)
            return;

        _modoDirecto = true;
        ActualizarModo();
    }

    private void ModoManual_Click(object? sender, RoutedEventArgs e)
    {
        _modoDirecto = false;
        ActualizarModo();
    }

    private void ActualizarModo()
    {
        BotonModoDirecto.Classes.Set("activo", _modoDirecto);
        BotonModoManual.Classes.Set("activo", !_modoDirecto);

        PanelManual.IsVisible = !_modoDirecto;

        BotonConfirmar.IsEnabled = TerminalActual is not null;

        BotonConfirmar.Content =
            _modoDirecto
                ? "Enviar cobro a la terminal"
                : "Registrar pago";

        MostrarMensaje(null);
    }


    private void Cerrar_Click(object? sender, RoutedEventArgs e) =>
        Terminar(false);


    // =========================================================
    // CONFIRMAR
    // =========================================================

    private async void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        var terminal = TerminalActual;

        if (terminal is null)
            return;

        if (_modoDirecto)
            await CobrarDirectoAsync(terminal);
        else
            ConfirmarManual(terminal);
    }


    private int IdFormaPagoSeleccionada =>
        RadioDebito.IsChecked == true
            ? FormasPagoPos.TarjetaDebito
            : FormasPagoPos.TarjetaCredito;


    // =========================================================
    // MANUAL
    // =========================================================

    private void ConfirmarManual(TerminalPagoConfiguration terminal)
    {
        var terminaciones = (CajaTerminaciones.Text ?? string.Empty).Trim();

        if (terminaciones.Length < 3 ||
            terminaciones.Length > 8 ||
            !terminaciones.All(char.IsDigit))
        {
            MostrarMensaje(
                "Captura las terminaciones del número de pago (de 3 a 8 dígitos, solo números).");

            CajaTerminaciones.Focus();
            return;
        }

        Resultado = new ResultadoCobroTarjeta
        {
            Aprobado = true,
            EsDirecto = false,
            IdFormaPago = IdFormaPagoSeleccionada,
            Importe = _importe,
            Referencia = terminaciones,
            Autorizacion = (CajaAutorizacion.Text ?? string.Empty).Trim(),
            NombreTerminal = terminal.Nombre,
            Mensaje = "Pago con tarjeta capturado manualmente."
        };

        Terminar(true);
    }


    // =========================================================
    // DIRECTO
    // =========================================================

    private async Task CobrarDirectoAsync(TerminalPagoConfiguration terminal)
    {
        if (_procesando)
            return;

        MostrarMensaje(null);

        EntrarEnProcesando(terminal, "Enviando el cobro a la terminal…");

        try
        {
            var envio = await _client.EnviarCobroAsync(
                terminal, _claveVenta, _folio, _importe,
                NovaCoreESDM.Models.Session.PosSession.Usuario);

            if (!envio.Ok || envio.Dato is null)
            {
                SalirDeProcesando(envio.SinComunicacion
                    ? envio.Mensaje + " Si la terminal ya mostró el importe, cancélalo ahí antes de reintentar."
                    : envio.Mensaje);

                return;
            }

            _referenciaEnCurso = envio.Dato.Referencia;

            await EsperarResultadoAsync(terminal, envio.Dato);
        }
        catch (Exception ex)
        {
            SalirDeProcesando($"No fue posible cobrar con la terminal: {ex.Message}");
        }
    }


    private async Task EsperarResultadoAsync(
        TerminalPagoConfiguration terminal,
        CobroTerminal cobro)
    {
        var fallosSeguidos = 0;

        while (true)
        {
            if (cobro.Estado == EstadoCobroTerminal.Aprobado)
            {
                // Nunca registrar un pago por un importe distinto al de la venta.
                if (cobro.ImporteCentavos != (long)Math.Round(_importe * 100m))
                {
                    TerminarIncierto(
                        "La terminal aprobó un importe distinto al de la venta. " +
                        "NO vuelvas a cobrar: verifica la operación " + cobro.Referencia + ".");
                    return;
                }

                var tarjeta =
                    string.IsNullOrWhiteSpace(cobro.Ultimos4)
                        ? string.Empty
                        : $" · {cobro.MarcaTarjeta} ****{cobro.Ultimos4}";

                // Lector del banco: trae los datos del pagaré y si la
                // tarjeta es de crédito o débito (manda sobre lo elegido).
                var detalle = DetalleTarjetaPago.Desde(cobro);

                var idFormaPago =
                    detalle?.Producto switch
                    {
                        "DEBITO" => FormasPagoPos.TarjetaDebito,
                        "CREDITO" => FormasPagoPos.TarjetaCredito,
                        _ => IdFormaPagoSeleccionada
                    };

                Resultado = new ResultadoCobroTarjeta
                {
                    Aprobado = true,
                    EsDirecto = true,
                    IdFormaPago = idFormaPago,
                    Detalle = detalle,
                    Importe = cobro.Importe,
                    Referencia = cobro.Referencia,
                    Autorizacion = cobro.Autorizacion ?? string.Empty,
                    NombreTerminal = terminal.Nombre,
                    Mensaje = $"Pago aprobado por la terminal{tarjeta}."
                };

                Terminar(true);
                return;
            }

            if (cobro.Estado == EstadoCobroTerminal.Rechazado)
            {
                var motivo =
                    string.IsNullOrWhiteSpace(cobro.MotivoRechazo)
                        ? CodigosRespuestaBanco.Texto(cobro.Banco?.CodigoRespuesta)
                        : cobro.MotivoRechazo;

                SalirDeProcesando(
                    "La terminal rechazó el pago" +
                    (string.IsNullOrWhiteSpace(motivo) ? "." : $": {motivo.TrimEnd('.')}."));
                return;
            }

            if (cobro.Estado == EstadoCobroTerminal.Cancelado)
            {
                SalirDeProcesando("El cobro fue cancelado en la terminal.");
                return;
            }

            if (cobro.Estado == EstadoCobroTerminal.Error)
            {
                SalirDeProcesando("La terminal reportó un error y no cobró.");
                return;
            }

            // PENDIENTE o TIMEOUT: seguimos consultando.
            if (cobro.Incierto)
            {
                TextoEstado.Text =
                    "La terminal no confirmó el resultado. Verificando… no vuelvas a cobrar.";
                BotonCancelarCobro.IsVisible = false;
                BotonCerrarIncierto.IsVisible = true;
            }
            else if (!_cancelando)
            {
                TextoEstado.Text = cobro.EtapaTexto;
            }

            await Task.Delay(1000);

            var consulta = await _client.ConsultarAsync(terminal, cobro.Referencia);

            if (consulta.Ok && consulta.Dato is not null)
            {
                fallosSeguidos = 0;
                cobro = consulta.Dato;
                continue;
            }

            fallosSeguidos++;

            TextoEstado.Text =
                "Sin comunicación con la terminal. Reintentando… " +
                "No vuelvas a cobrar: el pago pudo haberse procesado.";

            if (fallosSeguidos >= IntentosSinRespuestaParaCerrar)
            {
                BotonCancelarCobro.IsVisible = false;
                BotonCerrarIncierto.IsVisible = true;
            }
        }
    }


    private async void CancelarCobro_Click(object? sender, RoutedEventArgs e)
    {
        var terminal = TerminalActual;

        if (terminal is null || !_procesando || _cancelando)
            return;

        _cancelando = true;
        BotonCancelarCobro.IsEnabled = false;
        TextoEstado.Text = "Cancelando el cobro en la terminal…";

        var cancelacion = await CancelarEnTerminalAsync(terminal);

        if (!cancelacion)
        {
            // No se pudo cancelar (ya se estaba procesando): se sigue esperando el resultado real.
            _cancelando = false;
            BotonCancelarCobro.IsEnabled = true;
        }
        // Si se canceló, el ciclo de consulta recibe el estado CANCELADO y cierra el flujo.
    }

    private string? _referenciaEnCurso;

    private async Task<bool> CancelarEnTerminalAsync(TerminalPagoConfiguration terminal)
    {
        if (string.IsNullOrEmpty(_referenciaEnCurso))
            return false;

        var r = await _client.CancelarAsync(terminal, _referenciaEnCurso);

        if (!r.Ok)
        {
            TextoEstado.Text = r.Mensaje;
            return false;
        }

        return true;
    }


    private void CerrarIncierto_Click(object? sender, RoutedEventArgs e) =>
        TerminarIncierto(
            "No se pudo confirmar el resultado con la terminal. NO vuelvas a cobrar esta venta: " +
            "verifica en la terminal si el pago se procesó.");


    // =========================================================
    // ESTADOS DE LA VENTANA
    // =========================================================

    private void EntrarEnProcesando(TerminalPagoConfiguration terminal, string texto)
    {
        _procesando = true;
        _cancelando = false;

        PanelSeleccion.IsVisible = false;
        PanelProcesando.IsVisible = true;

        BotonCancelarCobro.IsVisible = true;
        BotonCancelarCobro.IsEnabled = true;
        BotonCerrarIncierto.IsVisible = false;

        TextoTerminalProcesando.Text = $"Terminal: {terminal.Nombre}";
        TextoEstado.Text = texto;
    }

    private void SalirDeProcesando(string mensaje)
    {
        _procesando = false;
        _cancelando = false;

        PanelProcesando.IsVisible = false;
        PanelSeleccion.IsVisible = true;

        MostrarMensaje(mensaje);
    }

    private void TerminarIncierto(string mensaje)
    {
        Resultado = new ResultadoCobroTarjeta
        {
            Aprobado = false,
            EsDirecto = true,
            Importe = _importe,
            RequiereVerificacion = true,
            Mensaje = mensaje
        };

        Terminar(false);
    }

    private void Terminar(bool aprobado)
    {
        _permitirCierre = true;
        _procesando = false;

        Close(aprobado);
    }

    private void MostrarMensaje(string? mensaje)
    {
        BannerMensaje.IsVisible = !string.IsNullOrWhiteSpace(mensaje);
        TextoMensaje.Text = mensaje;
    }
}
