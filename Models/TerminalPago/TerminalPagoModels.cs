using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.TerminalPago;

/// <summary>Estados que reporta la terminal.</summary>
public static class EstadoCobroTerminal
{
    public const string Pendiente = "PENDIENTE";
    public const string Aprobado = "APROBADO";
    public const string Rechazado = "RECHAZADO";
    public const string Cancelado = "CANCELADO";
    public const string Timeout = "TIMEOUT";
    public const string Error = "ERROR";
}

public class CobroTerminal
{
    [JsonPropertyName("reference")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("external_reference")]
    public string ReferenciaExterna { get; set; } = string.Empty;

    [JsonPropertyName("terminal")]
    public string Terminal { get; set; } = string.Empty;

    [JsonPropertyName("amount_cents")]
    public long ImporteCentavos { get; set; }

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("final")]
    public bool Final { get; set; }

    [JsonPropertyName("stage")]
    public string? Etapa { get; set; }

    [JsonPropertyName("card_brand")]
    public string? MarcaTarjeta { get; set; }

    [JsonPropertyName("card_last4")]
    public string? Ultimos4 { get; set; }

    [JsonPropertyName("auth_code")]
    public string? Autorizacion { get; set; }

    [JsonPropertyName("decline_reason")]
    public string? MotivoRechazo { get; set; }

    [JsonIgnore]
    public decimal Importe => ImporteCentavos / 100m;

    [JsonIgnore]
    public bool Aprobado => Estado == EstadoCobroTerminal.Aprobado;

    /// <summary>
    /// TIMEOUT = la terminal pudo haber cobrado y la respuesta se perdió.
    /// NUNCA se debe reintentar con otro cobro: hay que volver a consultar.
    /// </summary>
    [JsonIgnore]
    public bool Incierto => Estado == EstadoCobroTerminal.Timeout;

    [JsonIgnore]
    public bool Abierto =>
        Estado == EstadoCobroTerminal.Pendiente || Incierto;

    [JsonIgnore]
    public string EtapaTexto =>
        Etapa switch
        {
            "COBRO_SOLICITADO" => "Cobro enviado a la terminal…",
            "ESPERANDO_TARJETA" => "Esperando que el cliente acerque o inserte la tarjeta…",
            "TARJETA_DETECTADA" => "Tarjeta detectada. Esperando confirmación en la terminal…",
            "PROCESANDO" => "Procesando el pago con el banco…",
            _ => "Esperando respuesta de la terminal…"
        };
}

public class ResultadoTerminal<T>
{
    public bool Ok { get; init; }

    public T? Dato { get; init; }

    public string Mensaje { get; init; } = string.Empty;

    /// <summary>
    /// true = no hubo comunicación (el estado real es DESCONOCIDO, no un rechazo).
    /// </summary>
    public bool SinComunicacion { get; init; }

    public static ResultadoTerminal<T> Exito(T dato) =>
        new() { Ok = true, Dato = dato };

    public static ResultadoTerminal<T> Falla(string mensaje, bool sinComunicacion = false) =>
        new() { Ok = false, Mensaje = mensaje, SinComunicacion = sinComunicacion };
}

/// <summary>Resultado del flujo completo de cobro con tarjeta.</summary>
public class ResultadoCobroTarjeta
{
    public bool Aprobado { get; set; }

    /// <summary>true = cobro directo por terminal; false = captura manual.</summary>
    public bool EsDirecto { get; set; }

    public int IdFormaPago { get; set; }

    public decimal Importe { get; set; }

    /// <summary>Directo: referencia de la operación. Manual: terminaciones.</summary>
    public string Referencia { get; set; } = string.Empty;

    public string Autorizacion { get; set; } = string.Empty;

    public string NombreTerminal { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// true = quedó una operación incierta: NO volver a cobrar la venta.
    /// </summary>
    public bool RequiereVerificacion { get; set; }
}
