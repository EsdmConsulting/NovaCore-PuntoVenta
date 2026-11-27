namespace NovaCoreESDM.Models.TerminalPago;

/// <summary>
/// Códigos de respuesta del banco (BBVA TOTAL POS SDK, tabla "Códigos de
/// Respuesta") en palabras para el cajero. El puente ya manda el motivo;
/// esto es el respaldo si solo llega el código.
/// </summary>
public static class CodigosRespuestaBanco
{
    public static string Texto(string? codigo) =>
        (codigo ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "" => string.Empty,
            "00" => "Aprobada",
            "01" => "Llame al banco emisor de la tarjeta",
            "03" => "Terminal inválida: la afiliación no existe o está mal configurada",
            "04" or "05" => "Declinada por el banco emisor",
            "08" => "La afiliación no permite este tipo de operación",
            "12" => "Importe inválido",
            "14" => "Tarjeta inválida",
            "19" => "Problema de comunicación, intente de nuevo",
            "28" or "82" => "Servicio del banco no disponible, intente más tarde",
            "30" => "Error de formato en la operación",
            "33" => "Tarjeta vencida",
            "45" => "La afiliación no permite promociones",
            "46" => "El importe es menor al mínimo de la promoción",
            "48" => "Falta el código de seguridad (CVV)",
            "49" => "Código de seguridad (CVV) incorrecto",
            "51" => "Fondos insuficientes",
            "55" => "NIP incorrecto",
            "57" => "La afiliación no permite retiro de efectivo",
            "58" => "Operación no permitida para la afiliación",
            "61" or "65" => "Se excede el monto autorizado",
            "81" => "El banco emisor no tiene servicio",
            "87" => "La operación ya se había procesado",
            "88" => "Operación reversada",
            "RI" => "La terminal no está registrada en BBVA (afiliación, terminal o MAC incorrectos)",
            var otro => $"Código {otro} del banco"
        };
}
