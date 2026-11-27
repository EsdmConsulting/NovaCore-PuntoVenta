package mx.novacore.puente;

import java.util.LinkedHashMap;
import java.util.Map;

/**
 * Un cobro en el lector, con el formato del contrato api/v1 que ya usa el
 * POS (Services/TerminalPago/TerminalPagoClient.cs).
 *
 * Estados: PENDIENTE → APROBADO | RECHAZADO | CANCELADO | ERROR, o TIMEOUT
 * mientras no se sabe si el banco cobró (hasta que el reverso se confirma).
 */
public final class Cobro {

    public static final String PENDIENTE = "PENDIENTE";
    public static final String APROBADO = "APROBADO";
    public static final String RECHAZADO = "RECHAZADO";
    public static final String CANCELADO = "CANCELADO";
    public static final String TIMEOUT = "TIMEOUT";
    public static final String ERROR = "ERROR";

    public String referencia;
    public String referenciaExterna;
    public String terminal;
    public long centavos;
    public String moneda = "MXN";
    public String descripcion;
    public String operador;
    public String idempotencia;

    public String estado = PENDIENTE;
    public String etapa = "COBRO_SOLICITADO";
    public long creado;
    public long actualizado;

    public String marca;
    public String ultimos4;
    public String autorizacion;
    public String motivoRechazo;
    public Map<String, Object> banco;

    /** Cancelación o devolución en el banco (null si no hay). */
    public Map<String, Object> reverso;

    /** Volatil: el cajero pidió cancelar mientras se esperaba la tarjeta. */
    public transient volatile boolean cancelarSolicitado;

    public boolean esFinal() {
        return !PENDIENTE.equals(estado) && !TIMEOUT.equals(estado);
    }

    public boolean abierto() {
        return PENDIENTE.equals(estado) || TIMEOUT.equals(estado);
    }

    public Map<String, Object> aJson() {
        Map<String, Object> m = new LinkedHashMap<String, Object>();
        m.put("reference", referencia);
        m.put("external_reference", referenciaExterna);
        m.put("terminal", terminal);
        m.put("amount_cents", centavos);
        m.put("currency", moneda);
        m.put("description", descripcion);
        m.put("operator", operador);
        m.put("status", estado);
        m.put("final", esFinal());
        m.put("stage", PENDIENTE.equals(estado) ? etapa : null);
        m.put("card_brand", marca);
        m.put("card_last4", ultimos4);
        m.put("auth_code", autorizacion);
        m.put("decline_reason", motivoRechazo);
        m.put("bank", banco);
        m.put("void", reverso);
        m.put("created_at", creado);
        m.put("updated_at", actualizado);
        return m;
    }

    /** Para guardar en disco (incluye la clave de idempotencia). */
    public Map<String, Object> aDisco() {
        Map<String, Object> m = aJson();
        m.put("idempotency_key", idempotencia);
        return m;
    }

    @SuppressWarnings("unchecked")
    public static Cobro desdeDisco(Map<String, Object> m) {
        Cobro c = new Cobro();
        c.referencia = Json.texto(m, "reference");
        c.referenciaExterna = Json.texto(m, "external_reference");
        c.terminal = Json.texto(m, "terminal");
        c.centavos = Json.entero(m, "amount_cents", 0);
        c.moneda = Json.texto(m, "currency");
        c.descripcion = Json.texto(m, "description");
        c.operador = Json.texto(m, "operator");
        c.estado = Json.texto(m, "status");
        c.etapa = Json.texto(m, "stage");
        c.marca = Json.texto(m, "card_brand");
        c.ultimos4 = Json.texto(m, "card_last4");
        c.autorizacion = Json.texto(m, "auth_code");
        c.motivoRechazo = Json.texto(m, "decline_reason");
        c.banco = (Map<String, Object>) m.get("bank");
        c.reverso = (Map<String, Object>) m.get("void");
        c.creado = Json.entero(m, "created_at", 0);
        c.actualizado = Json.entero(m, "updated_at", 0);
        c.idempotencia = Json.texto(m, "idempotency_key");
        return c;
    }

    /** "123.45" a partir de centavos (el SDK recibe el importe con punto decimal). */
    public static String importeTexto(long centavos) {
        return String.format(java.util.Locale.ROOT, "%d.%02d", centavos / 100, centavos % 100);
    }
}
