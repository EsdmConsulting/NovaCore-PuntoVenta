package mx.novacore.puente;

/**
 * Quien realmente cobra: el SDK de BBVA (BbvaAutorizador) o el simulador
 * (SimuladorAutorizador). El resto del puente no sabe cuál es.
 */
public interface Autorizador {

    /** BBVA_TOTALPOS o SIMULADOR (se guarda en el ERP y se imprime "PRUEBA" si es simulador). */
    String procesador();

    /**
     * Arranque y cada cierto tiempo: el SDK de BBVA aquí carga bines, revisa
     * la carga de llaves y manda los REVERSOS pendientes (cobros sin respuesta).
     */
    void inicializar() throws Exception;

    /** true = listo para cobrar. */
    boolean listo();

    /** Texto para el cajero / soporte ("Listo", "Falta cargar llaves", ...). */
    String estado();

    /**
     * Venta: pide la tarjeta en el lector y la autoriza con el banco.
     * Bloquea hasta terminar. Nunca lanza por un rechazo del banco: lo
     * regresa en el resultado.
     */
    ResultadoBanco vender(Cobro cobro, Avance avance) throws Exception;

    /** El cajero canceló mientras el lector esperaba la tarjeta. */
    void cancelarLectura();

    /**
     * Cancelación (mismo día bancario, sin tarjeta) o devolución (con la
     * tarjeta del cliente) de un cobro aprobado.
     */
    ResultadoBanco reversar(Cobro original, boolean esCancelacion, long centavos, Avance avance) throws Exception;

    /** Carga de llaves manual del lector (obligatoria con lector nuevo). */
    ResultadoBanco cargarLlaves() throws Exception;

    /** Avance de la operación para que el POS lo muestre. */
    interface Avance {
        void etapa(String etapa);

        boolean cancelado();
    }
}
