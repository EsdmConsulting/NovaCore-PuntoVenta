using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace NovaCoreESDM.Views.Ventas;

/// <summary>
/// Pago con una terminal bancaria que no está conectada al punto de venta
/// (externa, o cualquiera cuando no hay internet): el cajero cobra en la
/// terminal y captura aquí la autorización del voucher.
/// </summary>
public partial class PagoTarjetaExternaWindow : Window
{
    private readonly decimal _saldo;

    public decimal Importe { get; private set; }
    public bool EsDebito { get; private set; }
    public string Autorizacion { get; private set; } = string.Empty;
    public string UltimosDigitos { get; private set; } = string.Empty;


    // Constructor sin parámetros para el diseñador.
    public PagoTarjetaExternaWindow() : this(0m, sinConexion: false) { }


    public PagoTarjetaExternaWindow(decimal saldo, bool sinConexion)
    {
        InitializeComponent();

        if (sinConexion)
        {
            TextoEncabezado.Text = "TARJETA · SIN CONEXIÓN";
            TextoInstrucciones.Text =
                "Sin internet el punto de venta no se comunica con la terminal. Cobra directo en la terminal bancaria y, cuando el pago salga aprobado, captura aquí los datos del voucher.";
        }

        _saldo = Math.Round(saldo, 2);

        TextoSaldo.Text = $"${_saldo:N2}";
        ImporteTextBox.Text = _saldo.ToString("0.00", CultureInfo.InvariantCulture);

        Opened += (_, _) =>
            Dispatcher.UIThread.Post(
                () =>
                {
                    AutorizacionTextBox.Focus(NavigationMethod.Tab);
                },
                DispatcherPriority.Input);
    }


    private void Cancelar_Click(object? sender, RoutedEventArgs e) =>
        Close(false);


    private void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        TextoMensaje.Text = string.Empty;

        var textoImporte = (ImporteTextBox.Text ?? string.Empty)
            .Replace("$", string.Empty)
            .Replace(",", string.Empty)
            .Trim();

        if (!decimal.TryParse(textoImporte, NumberStyles.Number, CultureInfo.InvariantCulture, out var importe) ||
            importe <= 0m)
        {
            Error("Escribe el importe que se cobró en la terminal.", ImporteTextBox);
            return;
        }

        importe = Math.Round(importe, 2);

        if (importe > _saldo)
        {
            Error($"El importe no puede ser mayor al saldo (${_saldo:N2}).", ImporteTextBox);
            return;
        }

        var autorizacion = (AutorizacionTextBox.Text ?? string.Empty).Trim().ToUpperInvariant();

        if (autorizacion.Length < 4 || !autorizacion.All(char.IsLetterOrDigit))
        {
            Error("Captura el número de autorización del voucher (mínimo 4 letras o números).", AutorizacionTextBox);
            return;
        }

        var digitos = (DigitosTextBox.Text ?? string.Empty).Trim();

        if (digitos.Length > 0 && (digitos.Length != 4 || !digitos.All(char.IsDigit)))
        {
            Error("Los últimos dígitos deben ser 4 números (o déjalo vacío).", DigitosTextBox);
            return;
        }

        Importe = importe;
        EsDebito = DebitoRadio.IsChecked == true;
        Autorizacion = autorizacion;
        UltimosDigitos = digitos;

        Close(true);
    }


    private void Error(string mensaje, TextBox campo)
    {
        TextoMensaje.Text = mensaje;
        campo.Focus();
        campo.SelectAll();
    }
}
