using System.Text.Json.Serialization;
using NovaCoreESDM.Models.TerminalPago;

namespace NovaCoreESDM.Models.Ventas;

/// <summary>
/// Datos que devolvió el banco en un cobro DIRECTO con el lector (BBVA
/// TOTAL POS a través del puente de la caja, o su simulador).
///
/// Se usa igual en tres lugares:
///  - "detalle_tarjeta" del pago (pagos.php / sincronizar.php del ERP),
///  - el pago guardado en la caja cuando no hay conexión con el ERP,
///  - el pagaré que se imprime con el ticket.
///
/// NUNCA lleva el número completo de la tarjeta: solo los últimos 4.
/// Los cobros capturados a mano no tienen detalle (null).
/// </summary>
public sealed class DetalleTarjetaPago
{
    [JsonPropertyName("procesador")]
    public string Procesador { get; set; } = string.Empty;

    /// <summary>Referencia del cobro en el puente: evita registrar dos veces el mismo cobro.</summary>
    [JsonPropertyName("referencia_operacion")]
    public string? ReferenciaOperacion { get; set; }

    [JsonPropertyName("afiliacion")]
    public string? Afiliacion { get; set; }

    [JsonPropertyName("numero_terminal")]
    public string? NumeroTerminal { get; set; }

    [JsonPropertyName("operacion")]
    public string? Operacion { get; set; }

    /// <summary>Nombre de la transacción como lo da el banco (p. ej. "VENTA").</summary>
    [JsonPropertyName("nombre_transaccion")]
    public string? NombreTransaccion { get; set; }

    [JsonPropertyName("moneda")]
    public string? Moneda { get; set; }

    [JsonPropertyName("autorizacion")]
    public string? Autorizacion { get; set; }

    [JsonPropertyName("codigo_respuesta")]
    public string? CodigoRespuesta { get; set; }

    [JsonPropertyName("leyenda")]
    public string? Leyenda { get; set; }

    [JsonPropertyName("referencia_financiera")]
    public string? ReferenciaFinanciera { get; set; }

    [JsonPropertyName("secuencia")]
    public string? Secuencia { get; set; }

    [JsonPropertyName("ultimos4")]
    public string? Ultimos4 { get; set; }

    [JsonPropertyName("marca")]
    public string? Marca { get; set; }

    /// <summary>CREDITO o DEBITO.</summary>
    [JsonPropertyName("producto")]
    public string? Producto { get; set; }

    [JsonPropertyName("emisor")]
    public string? Emisor { get; set; }

    [JsonPropertyName("tarjetahabiente")]
    public string? Tarjetahabiente { get; set; }

    /// <summary>05 chip, 01 digitada, 90 banda, 80 fallback, 07/91 sin contacto.</summary>
    [JsonPropertyName("modo_lectura")]
    public string? ModoLectura { get; set; }

    [JsonPropertyName("aid")]
    public string? Aid { get; set; }

    [JsonPropertyName("aplicacion")]
    public string? Aplicacion { get; set; }

    [JsonPropertyName("arqc")]
    public string? Arqc { get; set; }

    /// <summary>AUTOGRAFA, ELECTRONICA o SIN_FIRMA.</summary>
    [JsonPropertyName("firma")]
    public string? Firma { get; set; }

    [JsonPropertyName("fecha_banco")]
    public string? FechaBanco { get; set; }


    [JsonIgnore]
    public bool EsDebito => string.Equals(Producto, "DEBITO", System.StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool EsSimulador => string.Equals(Procesador, "SIMULADOR", System.StringComparison.OrdinalIgnoreCase);


    /// <summary>
    /// Formato de la transacción que exige BBVA en el pagaré:
    /// 05 → I@1 (chip), 01 → T@1 (digitada), 80/90 → D@1 (banda),
    /// 07/91 → C@1 (sin contacto).
    /// </summary>
    [JsonIgnore]
    public string FormatoTransaccion =>
        (ModoLectura ?? string.Empty).Trim() switch
        {
            "05" => "I@1",
            "01" => "T@1",
            "80" or "90" => "D@1",
            "07" or "91" => "C@1",
            _ => string.Empty
        };


    /// <summary>Desde el cobro aprobado que devolvió el puente. null si no trae datos del banco.</summary>
    public static DetalleTarjetaPago? Desde(CobroTerminal cobro)
    {
        var b = cobro.Banco;

        if (b is null || string.IsNullOrWhiteSpace(b.Procesador))
            return null;

        return new DetalleTarjetaPago
        {
            Procesador = b.Procesador.Trim().ToUpperInvariant(),
            ReferenciaOperacion = cobro.Referencia,
            Afiliacion = b.Afiliacion,
            NumeroTerminal = b.NumeroTerminal,
            Operacion = string.IsNullOrWhiteSpace(b.Operacion) ? "VENTA" : b.Operacion,
            NombreTransaccion = b.NombreTransaccion,
            Moneda = string.IsNullOrWhiteSpace(b.Moneda) ? "MXN" : b.Moneda,
            Autorizacion = cobro.Autorizacion,
            CodigoRespuesta = b.CodigoRespuesta,
            Leyenda = b.Leyenda,
            ReferenciaFinanciera = b.ReferenciaFinanciera,
            Secuencia = b.Secuencia,
            Ultimos4 = cobro.Ultimos4,
            Marca = cobro.MarcaTarjeta,
            Producto = NormalizarProducto(b.Producto),
            Emisor = b.Emisor,
            Tarjetahabiente = b.Tarjetahabiente,
            ModoLectura = b.ModoLectura,
            Aid = b.Aid,
            Aplicacion = b.Aplicacion,
            Arqc = b.Arqc,
            Firma = NormalizarFirma(b.Firma),
            FechaBanco = b.FechaHora
        };
    }


    public static string? NormalizarProducto(string? producto) =>
        (producto ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "C" or "CREDITO" or "CRÉDITO" or "CREDIT" => "CREDITO",
            "D" or "DEBITO" or "DÉBITO" or "DEBIT" => "DEBITO",
            _ => null
        };


    public static string? NormalizarFirma(string? firma) =>
        (firma ?? string.Empty).Trim().ToUpperInvariant().Replace(' ', '_') switch
        {
            "01" or "1" or "AUTOGRAFA" or "AUTÓGRAFA" => "AUTOGRAFA",
            "02" or "2" or "ELECTRONICA" or "ELECTRÓNICA" or "QPS" => "ELECTRONICA",
            "00" or "0" or "SIN_FIRMA" => "SIN_FIRMA",
            _ => null
        };
}
