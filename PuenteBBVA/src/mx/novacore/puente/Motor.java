package mx.novacore.puente;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.OutputStreamWriter;
import java.io.Writer;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.StandardCopyOption;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.TimeUnit;

/**
 * Cobros del lector: uno a la vez, guardados en disco (datos/cobros.json)
 * para que un reinicio no pierda un cobro aprobado ni uno sin confirmar.
 *
 * Reglas de dinero:
 *  - Nunca hay dos operaciones abiertas en el lector.
 *  - La misma clave de idempotencia regresa el mismo cobro (no cobra dos veces).
 *  - Si el banco no respondió (TIMEOUT), no se acepta otro cobro hasta que
 *    la inicialización del SDK mande el reverso; entonces queda ERROR
 *    ("no se cobró").
 */
public final class Motor {

    private static final long DIAS_GUARDADOS = 15;

    private final Autorizador autorizador;
    private final Config config;
    private final File archivo;
    private final Map<String, Cobro> cobros = new LinkedHashMap<String, Cobro>();
    private final ExecutorService trabajador = Executors.newSingleThreadExecutor(demonio("puente-lector"));
    private final ScheduledExecutorService reloj = Executors.newSingleThreadScheduledExecutor(demonio("puente-reloj"));
    private int consecutivo;
    private volatile String ultimoErrorInicializar;

    public Motor(Autorizador autorizador, Config config) {
        this.autorizador = autorizador;
        this.config = config;
        this.archivo = new File(config.datos, "cobros.json");
        cargar();
    }

    private static ThreadFactory demonio(final String nombre) {
        return new ThreadFactory() {
            public Thread newThread(Runnable r) {
                Thread t = new Thread(r, nombre);
                t.setDaemon(true);
                return t;
            }
        };
    }

    public Autorizador autorizador() {
        return autorizador;
    }

    public String ultimoErrorInicializar() {
        return ultimoErrorInicializar;
    }

    // ============================================================ arranque

    /** Inicializa el SDK ahora y luego cada 10 minutos (o cada 30 s si hay un cobro sin confirmar). */
    public void arrancar() {
        inicializarAhora();
        reloj.scheduleWithFixedDelay(new Runnable() {
            public void run() {
                if (hayInciertos() || !autorizador.listo()) {
                    trabajador.submit(new Runnable() {
                        public void run() {
                            inicializarAhora();
                        }
                    });
                }
            }
        }, 30, 30, TimeUnit.SECONDS);
        reloj.scheduleWithFixedDelay(new Runnable() {
            public void run() {
                trabajador.submit(new Runnable() {
                    public void run() {
                        inicializarAhora();
                    }
                });
            }
        }, 10, 10, TimeUnit.MINUTES);
    }

    private void inicializarAhora() {
        try {
            autorizador.inicializar();
            ultimoErrorInicializar = null;
            confirmarReversos();
        } catch (Exception e) {
            ultimoErrorInicializar = mensaje(e);
            Registro.aviso("No se pudo inicializar el lector: " + ultimoErrorInicializar);
        }
    }

    /** La inicialización mandó los reversos pendientes: los cobros sin confirmar no se cobraron. */
    private synchronized void confirmarReversos() {
        boolean cambio = false;
        for (Cobro c : cobros.values()) {
            if (Cobro.TIMEOUT.equals(c.estado)) {
                c.estado = Cobro.ERROR;
                c.motivoRechazo = "El banco no respondio. La operacion se reverso: NO se cobro. Puede cobrar de nuevo.";
                c.actualizado = System.currentTimeMillis();
                cambio = true;
                Registro.info("Cobro " + c.referencia + " sin respuesta: reverso enviado.");
            }
            if (c.reverso != null && Cobro.TIMEOUT.equals(c.reverso.get("status"))) {
                c.reverso.put("status", Cobro.ERROR);
                c.reverso.put("decline_reason", "El banco no respondio; se reverso. El cargo sigue vigente: intente de nuevo.");
                c.actualizado = System.currentTimeMillis();
                cambio = true;
            }
        }
        if (cambio) {
            guardar();
        }
    }

    private synchronized boolean hayInciertos() {
        for (Cobro c : cobros.values()) {
            if (Cobro.TIMEOUT.equals(c.estado) || (c.reverso != null && Cobro.TIMEOUT.equals(c.reverso.get("status")))) {
                return true;
            }
        }
        return false;
    }

    // ============================================================ consultas

    public synchronized Cobro obtener(String referencia) {
        Cobro c = cobros.get(referencia);
        if (c == null) {
            throw new PuenteError(404, "NOT_FOUND", "La operacion no existe en el lector.");
        }
        return c;
    }

    /** Más recientes primero; q busca en la referencia externa (clave de la venta del POS). */
    public synchronized List<Cobro> buscar(String q, int limite) {
        List<Cobro> lista = new ArrayList<Cobro>(cobros.values());
        Collections.reverse(lista);
        List<Cobro> salida = new ArrayList<Cobro>();
        for (Cobro c : lista) {
            if (q == null || q.isEmpty()
                    || (c.referenciaExterna != null && c.referenciaExterna.contains(q))
                    || c.referencia.equals(q)) {
                salida.add(c);
                if (salida.size() >= limite) {
                    break;
                }
            }
        }
        return salida;
    }

    private Cobro operacionAbierta() {
        for (Cobro c : cobros.values()) {
            if (c.abierto()) {
                return c;
            }
            if (c.reverso != null) {
                Object s = c.reverso.get("status");
                if (Cobro.PENDIENTE.equals(s) || Cobro.TIMEOUT.equals(s)) {
                    return c;
                }
            }
        }
        return null;
    }

    // ============================================================ cobrar

    public Cobro crear(String codigoTerminal, long centavos, String moneda, String externa,
                       String descripcion, String operador, String idempotencia) {

        if (codigoTerminal == null || !codigoTerminal.trim().equalsIgnoreCase(config.codigoTerminal())) {
            throw new PuenteError(404, "TERMINAL_NOT_FOUND", "No existe la terminal " + codigoTerminal + " en este lector.");
        }
        if (centavos <= 0 || centavos > 99999999L) {
            throw new PuenteError(422, "INVALID_AMOUNT", "El importe no es valido.");
        }
        if (moneda != null && !moneda.trim().isEmpty() && !moneda.trim().equalsIgnoreCase("MXN")) {
            throw new PuenteError(422, "INVALID_CURRENCY", "Solo se cobra en pesos (MXN).");
        }

        final Cobro c;

        synchronized (this) {
            if (idempotencia != null && !idempotencia.isEmpty()) {
                for (Cobro previo : cobros.values()) {
                    if (idempotencia.equals(previo.idempotencia)) {
                        if (previo.centavos != centavos) {
                            throw new PuenteError(409, "IDEMPOTENCY_MISMATCH", "La clave ya se uso con otro importe.");
                        }
                        return previo;
                    }
                }
            }

            Cobro abierta = operacionAbierta();
            if (abierta != null) {
                throw new PuenteError(409, "TERMINAL_BUSY",
                        Cobro.TIMEOUT.equals(abierta.estado)
                                ? "El lector tiene un cobro sin confirmar (" + abierta.referencia + "). Espere a que se reverse."
                                : "El lector ya tiene una operacion en curso (" + abierta.referencia + ").");
            }

            if (!autorizador.listo()) {
                throw new PuenteError(503, "TERMINAL_OFFLINE", autorizador.estado());
            }

            c = new Cobro();
            c.referencia = nuevaReferencia();
            c.referenciaExterna = recortar(externa, 60);
            c.terminal = config.codigoTerminal();
            c.centavos = centavos;
            c.moneda = "MXN";
            c.descripcion = recortar(descripcion, 80);
            c.operador = recortar(operador, 30);
            c.idempotencia = recortar(idempotencia, 120);
            c.creado = c.actualizado = System.currentTimeMillis();
            cobros.put(c.referencia, c);
            guardar();
        }

        Registro.info("Cobro " + c.referencia + " por " + Cobro.importeTexto(c.centavos) + " (" + c.referenciaExterna + ")");

        trabajador.submit(new Runnable() {
            public void run() {
                vender(c);
            }
        });

        return c;
    }

    private void vender(final Cobro c) {
        if (c.cancelarSolicitado) {
            terminar(c, ResultadoBanco.cancelado(), "VENTA");
            return;
        }

        ResultadoBanco r;
        try {
            r = autorizador.vender(c, avance(c, null));
        } catch (Exception e) {
            r = ResultadoBanco.conError(mensaje(e));
        }
        if (r == null) {
            r = ResultadoBanco.conError("El lector no regreso resultado.");
        }
        terminar(c, r, "VENTA");

        if (r.sinRespuesta) {
            // El SDK guardó el reverso: se manda con la siguiente inicialización.
            inicializarAhora();
        }
    }

    private Autorizador.Avance avance(final Cobro c, final Map<String, Object> reverso) {
        return new Autorizador.Avance() {
            public void etapa(String etapa) {
                synchronized (Motor.this) {
                    if (reverso == null) {
                        c.etapa = etapa;
                    } else {
                        reverso.put("stage", etapa);
                    }
                    c.actualizado = System.currentTimeMillis();
                }
            }

            public boolean cancelado() {
                return c.cancelarSolicitado;
            }
        };
    }

    private synchronized void terminar(Cobro c, ResultadoBanco r, String operacion) {
        c.actualizado = System.currentTimeMillis();
        c.banco = r.banco(autorizador.procesador(), operacion);

        if (r.cancelado) {
            c.estado = Cobro.CANCELADO;
            c.motivoRechazo = "Cancelado antes de pasar la tarjeta.";
        } else if (r.sinRespuesta) {
            c.estado = Cobro.TIMEOUT;
            c.motivoRechazo = "El banco no respondio. Se esta reversando; no cobre de nuevo todavia.";
        } else if (r.error != null) {
            c.estado = Cobro.ERROR;
            c.motivoRechazo = r.error;
        } else if (r.aprobado) {
            c.estado = Cobro.APROBADO;
            c.autorizacion = r.autorizacion;
            c.ultimos4 = r.ultimos4;
            c.marca = r.marca;
            c.motivoRechazo = null;
        } else {
            c.estado = Cobro.RECHAZADO;
            c.ultimos4 = r.ultimos4;
            c.marca = r.marca;
            c.motivoRechazo = CodigosRespuesta.texto(r.codigoRespuesta);
        }

        c.etapa = null;
        guardar();
        Registro.info("Cobro " + c.referencia + ": " + c.estado
                + (r.codigoRespuesta != null ? " (codigo " + r.codigoRespuesta + ")" : "")
                + (c.motivoRechazo != null && !Cobro.APROBADO.equals(c.estado) ? " " + c.motivoRechazo : ""));
    }

    // ============================================================ cancelar en espera

    public Cobro cancelar(String referencia) {
        Cobro c;
        synchronized (this) {
            c = obtener(referencia);

            Map<String, Object> rev = c.reverso;
            boolean reversoEnEspera = rev != null && Cobro.PENDIENTE.equals(rev.get("status"));

            if (!Cobro.PENDIENTE.equals(c.estado) && !reversoEnEspera) {
                throw new PuenteError(409, "INVALID_TRANSITION", "La operacion ya no puede cancelarse.");
            }

            String etapa = reversoEnEspera ? String.valueOf(rev.get("stage")) : c.etapa;
            if ("PROCESANDO".equals(etapa)) {
                throw new PuenteError(409, "IN_FLIGHT", "La operacion ya se esta procesando con el banco.");
            }
            c.cancelarSolicitado = true;
        }
        autorizador.cancelarLectura();
        return c;
    }

    // ============================================================ cancelación / devolución

    /**
     * Regresa el cargo de un cobro aprobado: CANCELACION si sigue el mismo día
     * bancario y es por el total; si no, DEVOLUCION (pide la tarjeta).
     */
    public Cobro reversar(String referencia, long centavos, String operador) {
        final Cobro c;
        final boolean esCancelacion;
        final long importe;
        final Map<String, Object> rev;

        synchronized (this) {
            c = obtener(referencia);

            if (!Cobro.APROBADO.equals(c.estado)) {
                throw new PuenteError(409, "INVALID_TRANSITION", "Solo se puede regresar un cobro aprobado.");
            }
            if (c.reverso != null && Cobro.APROBADO.equals(c.reverso.get("status"))) {
                throw new PuenteError(409, "ALREADY_VOIDED", "Ese cobro ya se cancelo o devolvio.");
            }
            Cobro abierta = operacionAbierta();
            if (abierta != null) {
                throw new PuenteError(409, "TERMINAL_BUSY", "El lector ya tiene una operacion en curso (" + abierta.referencia + ").");
            }
            if (!autorizador.listo()) {
                throw new PuenteError(503, "TERMINAL_OFFLINE", autorizador.estado());
            }

            importe = centavos > 0 ? centavos : c.centavos;
            if (importe > c.centavos) {
                throw new PuenteError(422, "INVALID_AMOUNT", "El importe a regresar es mayor al cobrado.");
            }

            esCancelacion = importe == c.centavos && DiaBancario.sePuedeCancelar(c.creado, System.currentTimeMillis());

            rev = new LinkedHashMap<String, Object>();
            rev.put("type", esCancelacion ? "CANCELACION" : "DEVOLUCION");
            rev.put("status", Cobro.PENDIENTE);
            rev.put("stage", esCancelacion ? "PROCESANDO" : "ESPERANDO_TARJETA");
            rev.put("amount_cents", importe);
            rev.put("operator", recortar(operador, 30));
            rev.put("requested_at", System.currentTimeMillis());
            c.reverso = rev;
            c.cancelarSolicitado = false;
            c.actualizado = System.currentTimeMillis();
            guardar();
        }

        Registro.info((esCancelacion ? "Cancelacion" : "Devolucion") + " de " + c.referencia + " por " + Cobro.importeTexto(importe));

        trabajador.submit(new Runnable() {
            public void run() {
                ResultadoBanco r;
                try {
                    r = autorizador.reversar(c, esCancelacion, importe, avance(c, rev));
                } catch (Exception e) {
                    r = ResultadoBanco.conError(mensaje(e));
                }
                terminarReverso(c, rev, r);
                if (r != null && r.sinRespuesta) {
                    inicializarAhora();
                }
            }
        });

        return c;
    }

    private synchronized void terminarReverso(Cobro c, Map<String, Object> rev, ResultadoBanco r) {
        if (r == null) {
            r = ResultadoBanco.conError("El lector no regreso resultado.");
        }
        String estado;
        String motivo = null;
        if (r.cancelado) {
            estado = Cobro.CANCELADO;
            motivo = "Cancelado antes de pasar la tarjeta.";
        } else if (r.sinRespuesta) {
            estado = Cobro.TIMEOUT;
            motivo = "El banco no respondio. Se esta verificando.";
        } else if (r.error != null) {
            estado = Cobro.ERROR;
            motivo = r.error;
        } else if (r.aprobado) {
            estado = Cobro.APROBADO;
        } else {
            estado = Cobro.RECHAZADO;
            motivo = CodigosRespuesta.texto(r.codigoRespuesta);
        }
        rev.put("status", estado);
        rev.put("stage", null);
        rev.put("auth_code", r.autorizacion);
        rev.put("financial_reference", r.referenciaFinanciera);
        rev.put("response_code", r.codigoRespuesta);
        rev.put("decline_reason", motivo);
        rev.put("bank", r.banco(autorizador.procesador(), String.valueOf(rev.get("type"))));
        rev.put("finished_at", System.currentTimeMillis());
        c.actualizado = System.currentTimeMillis();
        guardar();
        Registro.info("Reverso de " + c.referencia + ": " + estado + (motivo != null ? " " + motivo : ""));
    }

    // ============================================================ llaves

    public ResultadoBanco cargarLlaves() throws Exception {
        synchronized (this) {
            Cobro abierta = operacionAbierta();
            if (abierta != null) {
                throw new PuenteError(409, "TERMINAL_BUSY", "El lector tiene una operacion en curso.");
            }
        }
        ResultadoBanco r = trabajador.submit(new java.util.concurrent.Callable<ResultadoBanco>() {
            public ResultadoBanco call() throws Exception {
                ResultadoBanco res = autorizador.cargarLlaves();
                inicializarAhora();
                return res;
            }
        }).get(180, TimeUnit.SECONDS);
        return r;
    }

    // ============================================================ disco

    private String nuevaReferencia() {
        consecutivo++;
        return "BB" + new SimpleDateFormat("yyyyMMddHHmmss").format(new Date()) + "-" + String.format("%04d", consecutivo % 10000);
    }

    @SuppressWarnings("unchecked")
    private void cargar() {
        if (!archivo.isFile()) {
            return;
        }
        try {
            String texto = new String(Files.readAllBytes(archivo.toPath()), StandardCharsets.UTF_8);
            Object o = Json.leer(texto);
            if (!(o instanceof List)) {
                return;
            }
            for (Object item : (List<Object>) o) {
                if (!(item instanceof Map)) {
                    continue;
                }
                Cobro c = Cobro.desdeDisco((Map<String, Object>) item);
                if (c.referencia == null) {
                    continue;
                }
                // Se apagó a la mitad: si ya iba con el banco no se sabe si cobró.
                if (Cobro.PENDIENTE.equals(c.estado)) {
                    if ("PROCESANDO".equals(c.etapa) || "TARJETA_DETECTADA".equals(c.etapa)) {
                        c.estado = Cobro.TIMEOUT;
                        c.motivoRechazo = "El puente se reinicio durante el cobro. Se esta verificando con el banco.";
                    } else {
                        c.estado = Cobro.ERROR;
                        c.motivoRechazo = "El puente se reinicio antes de pasar la tarjeta. No se cobro.";
                    }
                    c.etapa = null;
                }
                if (c.reverso != null && Cobro.PENDIENTE.equals(c.reverso.get("status"))) {
                    c.reverso.put("status", "PROCESANDO".equals(c.reverso.get("stage")) ? Cobro.TIMEOUT : Cobro.ERROR);
                    c.reverso.put("stage", null);
                }
                cobros.put(c.referencia, c);
            }
            consecutivo = cobros.size();
        } catch (Exception e) {
            Registro.aviso("No se pudo leer " + archivo + ": " + mensaje(e));
        }
    }

    private synchronized void guardar() {
        long limite = System.currentTimeMillis() - DIAS_GUARDADOS * 24L * 3600L * 1000L;
        List<Object> lista = new ArrayList<Object>();
        List<String> viejos = new ArrayList<String>();
        for (Cobro c : cobros.values()) {
            if (c.actualizado < limite && c.esFinal()) {
                viejos.add(c.referencia);
                continue;
            }
            lista.add(c.aDisco());
        }
        for (String r : viejos) {
            cobros.remove(r);
        }
        File tmp = new File(archivo.getPath() + ".tmp");
        try {
            Writer w = new OutputStreamWriter(new FileOutputStream(tmp), StandardCharsets.UTF_8);
            try {
                w.write(Json.escribir(lista));
            } finally {
                w.close();
            }
            Files.move(tmp.toPath(), archivo.toPath(), StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.ATOMIC_MOVE);
        } catch (IOException e) {
            Registro.aviso("No se pudo guardar " + archivo + ": " + mensaje(e));
        }
    }

    // ============================================================ utilidades

    static String recortar(String s, int max) {
        if (s == null) {
            return null;
        }
        String t = s.trim();
        return t.length() <= max ? t : t.substring(0, max);
    }

    static String mensaje(Throwable e) {
        Throwable t = e;
        while (t instanceof java.lang.reflect.InvocationTargetException && t.getCause() != null) {
            t = t.getCause();
        }
        String m = t.getMessage();
        return m == null || m.trim().isEmpty() ? t.getClass().getSimpleName() : m.trim();
    }
}
