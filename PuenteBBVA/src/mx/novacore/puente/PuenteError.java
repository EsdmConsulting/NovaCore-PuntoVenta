package mx.novacore.puente;

/** Error del contrato api/v1: {"error": {"code": ..., "message": ...}} con su estado HTTP. */
public final class PuenteError extends RuntimeException {

    public final int http;
    public final String codigo;

    public PuenteError(int http, String codigo, String mensaje) {
        super(mensaje);
        this.http = http;
        this.codigo = codigo;
    }
}
