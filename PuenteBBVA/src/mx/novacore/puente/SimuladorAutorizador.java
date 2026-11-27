package mx.novacore.puente;

import java.security.SecureRandom;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Simulador: se comporta como el lector y el banco, sin cobrar nada.
 * Sirve para probar el POS de punta a punta sin el kit de BBVA.
 *
 * El resultado se elige con los CENTAVOS del importe:
 *   .51  fondos insuficientes (51)        .05  declinada (05)
 *   .33  tarjeta vencida (33)             .55  NIP incorrecto (55)
 *   .19  el banco no responde (TIMEOUT → se reversa sola a los ~30 s)
 *   .02  aprobada, tarjeta de DÉBITO      .01  aprobada, firma AUTÓGRAFA (banda)
 *   .07  aprobada, sin contacto (sin firma)
 *   cualquier otro: aprobada, CRÉDITO con chip y NIP (firma electrónica)
 *
 * Todo lo que imprime el POS con el simulador dice "PRUEBA - SIN VALOR".
 */
public final class SimuladorAutorizador implements Autorizador {

    private final SecureRandom azar = new SecureRandom();
    private final Config config;
    private final long pausa;
    private int secuencia = 1000;

    /** Tras un ".19" la "red" falla un rato, como pasaría de verdad. */
    private volatile long sinRedHasta;

    public SimuladorAutorizador(Config config) {
        this.config = config;
        this.pausa = config.bandera("simulador.rapido", false) ? 150 : 1000;
    }

    public String procesador() {
        return "SIMULADOR";
    }

    public void inicializar() throws Exception {
        if (System.currentTimeMillis() < sinRedHasta) {
            throw new Exception("Sin comunicacion con el banco (simulado).");
        }
    }

    public boolean listo() {
        return true;
    }

    public String estado() {
        return "Simulador listo (no cobra de verdad).";
    }

    public void cancelarLectura() {
        // el ciclo de espera revisa avance.cancelado()
    }

    private boolean esperar(Autorizador.Avance avance, long ms) throws InterruptedException {
        long fin = System.currentTimeMillis() + ms;
        while (System.currentTimeMillis() < fin) {
            if (avance.cancelado()) {
                return false;
            }
            Thread.sleep(50);
        }
        return !avance.cancelado();
    }

    public ResultadoBanco vender(Cobro cobro, Autorizador.Avance avance) throws Exception {
        avance.etapa("ESPERANDO_TARJETA");
        if (!esperar(avance, 3 * pausa)) {
            return ResultadoBanco.cancelado();
        }
        avance.etapa("TARJETA_DETECTADA");
        if (!esperar(avance, pausa)) {
            return ResultadoBanco.cancelado();
        }
        avance.etapa("PROCESANDO");
        Thread.sleep(2 * pausa);

        int centavos = (int) (cobro.centavos % 100);
        ResultadoBanco r = base("VENTA");

        boolean debito = centavos == 2;
        r.producto = debito ? "DEBITO" : "CREDITO";
        r.marca = debito ? "MASTERCARD" : "VISA";
        r.ultimos4 = debito ? "5581" : "7899";
        r.aid = debito ? "A0000000041010" : "A0000000031010";
        r.aplicacion = debito ? "DEBIT MASTERCARD" : "VISA CREDITO";
        r.modoLectura = "05";
        r.firma = "ELECTRONICA";

        if (centavos == 1) {
            r.modoLectura = "90";
            r.firma = "AUTOGRAFA";
            r.arqc = null;
            r.aid = null;
            r.aplicacion = null;
        } else if (centavos == 7) {
            r.modoLectura = "07";
            r.firma = "SIN_FIRMA";
        }

        switch (centavos) {
            case 51: return rechazo(r, "51");
            case 5: return rechazo(r, "05");
            case 33: return rechazo(r, "33");
            case 55: return rechazo(r, "55");
            case 19:
                sinRedHasta = System.currentTimeMillis() + 20 * pausa;
                ResultadoBanco sin = new ResultadoBanco();
                sin.sinRespuesta = true;
                return sin;
            default:
                break;
        }

        r.aprobado = true;
        r.codigoRespuesta = "00";
        r.leyenda = "APROBADA";
        r.autorizacion = digitos(6);
        return r;
    }

    public ResultadoBanco reversar(Cobro original, boolean esCancelacion, long centavos, Autorizador.Avance avance)
            throws Exception {
        if (!esCancelacion) {
            avance.etapa("ESPERANDO_TARJETA");
            if (!esperar(avance, 3 * pausa)) {
                return ResultadoBanco.cancelado();
            }
            avance.etapa("PROCESANDO");
        }
        Thread.sleep(2 * pausa);
        ResultadoBanco r = base(esCancelacion ? "CANCELACION" : "DEVOLUCION");
        r.aprobado = true;
        r.codigoRespuesta = "00";
        r.leyenda = "APROBADA";
        r.autorizacion = digitos(6);
        r.ultimos4 = original.ultimos4;
        r.marca = original.marca;
        return r;
    }

    public ResultadoBanco cargarLlaves() throws Exception {
        Thread.sleep(pausa);
        ResultadoBanco r = base("CARGA_LLAVES");
        r.aprobado = true;
        r.codigoRespuesta = "00";
        r.leyenda = "LLAVES CARGADAS (SIMULADOR)";
        return r;
    }

    private ResultadoBanco base(String operacion) {
        ResultadoBanco r = new ResultadoBanco();
        r.afiliacion = config.afiliacion().isEmpty() ? "0000000" : config.afiliacion();
        r.numeroTerminal = config.numeroTerminal().isEmpty() ? "00000001" : config.numeroTerminal();
        r.nombreTransaccion = operacion;
        r.moneda = "MXN";
        r.referenciaFinanciera = digitos(12);
        r.secuencia = String.valueOf(++secuencia);
        r.emisor = "BANCO DE PRUEBA";
        r.tarjetahabiente = "CLIENTE/PRUEBA";
        r.arqc = hex(16);
        r.fechaHora = new SimpleDateFormat("dd/MM/yyyy HH:mm:ss").format(new Date());
        return r;
    }

    private ResultadoBanco rechazo(ResultadoBanco r, String codigo) {
        r.aprobado = false;
        r.codigoRespuesta = codigo;
        r.leyenda = CodigosRespuesta.texto(codigo).toUpperCase();
        return r;
    }

    private String digitos(int n) {
        StringBuilder sb = new StringBuilder();
        for (int k = 0; k < n; k++) {
            sb.append(azar.nextInt(10));
        }
        return sb.toString();
    }

    private String hex(int n) {
        StringBuilder sb = new StringBuilder();
        for (int k = 0; k < n; k++) {
            sb.append("0123456789ABCDEF".charAt(azar.nextInt(16)));
        }
        return sb.toString();
    }
}
