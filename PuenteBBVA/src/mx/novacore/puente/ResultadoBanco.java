package mx.novacore.puente;

import java.util.LinkedHashMap;
import java.util.Map;

/** Respuesta del banco a una operación (venta, cancelación, devolución, llaves). */
public final class ResultadoBanco {

    public boolean aprobado;

    /** El cajero canceló antes de pasar la tarjeta. */
    public boolean cancelado;

    /**
     * No hubo respuesta del banco (se cayó la red a la mitad). El SDK guarda
     * un reverso y lo manda en la siguiente inicialización.
     */
    public boolean sinRespuesta;

    /** Error antes de llegar al banco (lector desconectado, sin llaves, configuración...). */
    public String error;

    public String codigoRespuesta;
    public String leyenda;
    public String autorizacion;
    public String referenciaFinanciera;
    public String secuencia;
    public String ultimos4;
    public String marca;
    public String producto;
    public String emisor;
    public String tarjetahabiente;
    public String modoLectura;
    public String aid;
    public String aplicacion;
    public String arqc;
    public String firma;
    public String fechaHora;
    public String afiliacion;
    public String numeroTerminal;
    public String nombreTransaccion;
    public String moneda;

    public static ResultadoBanco conError(String mensaje) {
        ResultadoBanco r = new ResultadoBanco();
        r.error = mensaje;
        return r;
    }

    public static ResultadoBanco cancelado() {
        ResultadoBanco r = new ResultadoBanco();
        r.cancelado = true;
        return r;
    }

    /** Objeto "bank" del contrato api/v1 (lo lee el POS). Nunca lleva el número completo. */
    public Map<String, Object> banco(String procesador, String operacion) {
        Map<String, Object> m = new LinkedHashMap<String, Object>();
        m.put("processor", procesador);
        m.put("affiliation", afiliacion);
        m.put("terminal_number", numeroTerminal);
        m.put("operation", operacion);
        m.put("transaction_name", nombreTransaccion);
        m.put("currency", moneda == null ? "MXN" : moneda);
        m.put("response_code", codigoRespuesta);
        m.put("legend", leyenda);
        m.put("financial_reference", referenciaFinanciera);
        m.put("sequence", secuencia);
        m.put("card_product", producto);
        m.put("issuer", emisor);
        m.put("cardholder", tarjetahabiente);
        m.put("entry_mode", modoLectura);
        m.put("aid", aid);
        m.put("app_label", aplicacion);
        m.put("arqc", arqc);
        m.put("signature", firma);
        m.put("bank_datetime", fechaHora);
        return m;
    }

    /** Solo los últimos 4 dígitos de lo que venga (enmascarado o no). */
    public static String ultimos4(String numero) {
        if (numero == null) {
            return null;
        }
        String d = numero.replaceAll("\\D+", "");
        return d.length() >= 4 ? d.substring(d.length() - 4) : null;
    }

    /** Marca por el primer dígito, si el banco lo manda (4 VISA, 5/2 MASTERCARD, 3 AMEX). */
    public static String marcaPorNumero(String numero) {
        if (numero == null) {
            return null;
        }
        String t = numero.trim();
        if (t.isEmpty()) {
            return null;
        }
        switch (t.charAt(0)) {
            case '4': return "VISA";
            case '5':
            case '2': return "MASTERCARD";
            case '3': return "AMEX";
            default: return null;
        }
    }
}
