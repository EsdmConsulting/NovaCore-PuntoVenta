package mx.novacore.puente;

/**
 * Códigos de respuesta del banco (TOTAL POS SDK, "Códigos de Respuesta")
 * en palabras para el cajero.
 */
public final class CodigosRespuesta {

    private CodigosRespuesta() {
    }

    public static String texto(String codigo) {
        String c = codigo == null ? "" : codigo.trim().toUpperCase();
        if (c.isEmpty()) {
            return "Sin respuesta del banco";
        }
        if (c.equals("00")) return "Aprobada";
        if (c.equals("01")) return "Llame al banco emisor de la tarjeta";
        if (c.equals("03")) return "Terminal invalida: la afiliacion no existe o esta mal configurada";
        // 04 "retenga y llame" se debe mostrar como declinada (regla del banco).
        if (c.equals("04") || c.equals("05")) return "Declinada por el banco emisor";
        if (c.equals("08")) return "La afiliacion no permite este tipo de operacion";
        if (c.equals("12")) return "Importe invalido";
        if (c.equals("14")) return "Tarjeta invalida";
        if (c.equals("19")) return "Problema de comunicacion, intente de nuevo";
        if (c.equals("28") || c.equals("82")) return "Servicio del banco no disponible, intente mas tarde";
        if (c.equals("30")) return "Error de formato en la operacion";
        if (c.equals("33")) return "Tarjeta vencida";
        if (c.equals("45")) return "La afiliacion no permite promociones";
        if (c.equals("46")) return "El importe es menor al minimo de la promocion";
        if (c.equals("48")) return "Falta el codigo de seguridad (CVV)";
        if (c.equals("49")) return "Codigo de seguridad (CVV) incorrecto";
        if (c.equals("51")) return "Fondos insuficientes";
        if (c.equals("55")) return "NIP incorrecto";
        if (c.equals("57")) return "La afiliacion no permite retiro de efectivo";
        if (c.equals("58")) return "Operacion no permitida para la afiliacion";
        if (c.equals("61") || c.equals("65")) return "Se excede el monto autorizado";
        if (c.equals("81")) return "El banco emisor no tiene servicio";
        if (c.equals("87")) return "La operacion ya se habia procesado";
        if (c.equals("88")) return "Operacion reversada";
        if (c.equals("RI")) return "La terminal no esta registrada en BBVA (afiliacion, terminal o MAC incorrectos)";
        return "Codigo " + c + " del banco";
    }
}
