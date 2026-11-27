package mx.novacore.puente;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetSocketAddress;
import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.Executors;

/**
 * API local del puente, con el mismo contrato que ya usa el POS para el
 * cobro directo (Services/TerminalPago/TerminalPagoClient.cs):
 *
 *   GET  /api/v1/health                       (sin clave)
 *   GET  /api/v1/terminals
 *   GET  /api/v1/payments?limit=&q=
 *   POST /api/v1/payments                     {terminal_code, amount_cents, currency,
 *                                              external_reference, description, operator}
 *                                             + cabecera Idempotency-Key
 *   GET  /api/v1/payments/{ref}
 *   POST /api/v1/payments/{ref}/cancel        cancela mientras se espera la tarjeta
 *   POST /api/v1/payments/{ref}/void          {amount_cents?, operator?} cancelación
 *                                             o devolución en el banco
 *   POST /api/v1/admin/load-keys              carga de llaves manual
 *
 * Todas (menos health) piden "Authorization: Bearer {clave}".
 * Respuestas: {"data": ...} o {"error": {"code": ..., "message": ...}}.
 */
public final class Servidor {

    private static final String PREFIJO = "/api/v1/";

    private final Motor motor;
    private final Config config;
    private final String clave;
    private HttpServer http;

    public Servidor(Motor motor, Config config) throws IOException {
        this.motor = motor;
        this.config = config;
        this.clave = config.clave();
    }

    public void iniciar() throws IOException {
        http = HttpServer.create(new InetSocketAddress(config.host(), config.puerto()), 20);
        http.setExecutor(Executors.newFixedThreadPool(4));
        http.createContext("/", new HttpHandler() {
            public void handle(HttpExchange ex) throws IOException {
                atender(ex);
            }
        });
        http.start();
    }

    public void detener() {
        if (http != null) {
            http.stop(0);
        }
    }

    // ============================================================ ruteo

    private void atender(HttpExchange ex) throws IOException {
        try {
            String metodo = ex.getRequestMethod().toUpperCase();
            String ruta = ex.getRequestURI().getPath();

            if (!ruta.startsWith(PREFIJO)) {
                throw new PuenteError(404, "NOT_FOUND", "Ruta no encontrada.");
            }
            String r = ruta.substring(PREFIJO.length());
            while (r.endsWith("/")) {
                r = r.substring(0, r.length() - 1);
            }

            if (r.equals("health") && metodo.equals("GET")) {
                responder(ex, 200, datos(salud()));
                return;
            }

            autenticar(ex);

            String[] partes = r.split("/");

            if (r.equals("terminals") && metodo.equals("GET")) {
                List<Object> lista = new ArrayList<Object>();
                lista.add(terminal());
                responder(ex, 200, datos(lista));
                return;
            }

            if (r.equals("payments") && metodo.equals("GET")) {
                Map<String, String> q = consulta(ex);
                int limite = 50;
                try {
                    limite = Math.max(1, Math.min(200, Integer.parseInt(valor(q, "limit", "50"))));
                } catch (NumberFormatException ignorado) {
                    // 50
                }
                List<Object> lista = new ArrayList<Object>();
                for (Cobro c : motor.buscar(valor(q, "q", ""), limite)) {
                    lista.add(c.aJson());
                }
                responder(ex, 200, datos(lista));
                return;
            }

            if (r.equals("payments") && metodo.equals("POST")) {
                Map<String, Object> b = cuerpo(ex);
                Cobro c = motor.crear(
                        Json.texto(b, "terminal_code"),
                        Json.entero(b, "amount_cents", 0),
                        Json.texto(b, "currency"),
                        Json.texto(b, "external_reference"),
                        Json.texto(b, "description"),
                        Json.texto(b, "operator"),
                        ex.getRequestHeaders().getFirst("Idempotency-Key"));
                responder(ex, 201, datos(c.aJson()));
                return;
            }

            if (partes.length == 2 && partes[0].equals("payments") && metodo.equals("GET")) {
                responder(ex, 200, datos(motor.obtener(decodificar(partes[1])).aJson()));
                return;
            }

            if (partes.length == 3 && partes[0].equals("payments") && metodo.equals("POST")) {
                String ref = decodificar(partes[1]);
                if (partes[2].equals("cancel")) {
                    responder(ex, 200, datos(motor.cancelar(ref).aJson()));
                    return;
                }
                if (partes[2].equals("void")) {
                    Map<String, Object> b = cuerpo(ex);
                    Cobro c = motor.reversar(ref, Json.entero(b, "amount_cents", 0), Json.texto(b, "operator"));
                    responder(ex, 202, datos(c.aJson()));
                    return;
                }
            }

            if (r.equals("admin/load-keys") && metodo.equals("POST")) {
                ResultadoBanco res = motor.cargarLlaves();
                Map<String, Object> m = new LinkedHashMap<String, Object>();
                m.put("ok", res.aprobado);
                m.put("response_code", res.codigoRespuesta);
                m.put("message", res.aprobado ? "Llaves cargadas." : CodigosRespuesta.texto(res.codigoRespuesta));
                responder(ex, 200, datos(m));
                return;
            }

            throw new PuenteError(404, "NOT_FOUND", "Ruta no encontrada.");
        } catch (PuenteError e) {
            error(ex, e.http, e.codigo, e.getMessage());
        } catch (IllegalArgumentException e) {
            error(ex, 400, "BAD_REQUEST", e.getMessage());
        } catch (Exception e) {
            Registro.aviso("Error en " + ex.getRequestURI().getPath() + ": " + Motor.mensaje(e));
            error(ex, 500, "INTERNAL", "Error interno del puente: " + Motor.mensaje(e));
        } finally {
            ex.close();
        }
    }

    private Map<String, Object> salud() {
        Map<String, Object> m = new LinkedHashMap<String, Object>();
        m.put("status", "ok");
        m.put("processor", motor.autorizador().procesador());
        m.put("ready", motor.autorizador().listo());
        m.put("message", motor.autorizador().estado());
        m.put("last_init_error", motor.ultimoErrorInicializar());
        m.put("version", Main.VERSION);
        return m;
    }

    private Map<String, Object> terminal() {
        Map<String, Object> t = new LinkedHashMap<String, Object>();
        t.put("code", config.codigoTerminal());
        t.put("name", config.nombreTerminal());
        t.put("active", true);
        t.put("connected", motor.autorizador().listo());
        t.put("processor", motor.autorizador().procesador());
        t.put("affiliation", config.afiliacion());
        t.put("terminal_number", config.numeroTerminal());
        return t;
    }

    // ============================================================ utilidades

    private void autenticar(HttpExchange ex) {
        String h = ex.getRequestHeaders().getFirst("Authorization");
        String dada = h != null && h.startsWith("Bearer ") ? h.substring(7).trim() : "";
        if (!MessageDigest.isEqual(dada.getBytes(StandardCharsets.UTF_8), clave.getBytes(StandardCharsets.UTF_8))) {
            throw new PuenteError(401, "UNAUTHENTICATED", "Clave de acceso incorrecta.");
        }
    }

    private static Map<String, Object> datos(Object d) {
        Map<String, Object> m = new LinkedHashMap<String, Object>();
        m.put("data", d);
        return m;
    }

    private static void error(HttpExchange ex, int http, String codigo, String mensaje) throws IOException {
        Map<String, Object> e = new LinkedHashMap<String, Object>();
        e.put("code", codigo);
        e.put("message", mensaje);
        Map<String, Object> m = new LinkedHashMap<String, Object>();
        m.put("error", e);
        responder(ex, http, m);
    }

    private static void responder(HttpExchange ex, int http, Object cuerpo) throws IOException {
        byte[] b = Json.escribir(cuerpo).getBytes(StandardCharsets.UTF_8);
        ex.getResponseHeaders().set("Content-Type", "application/json; charset=utf-8");
        ex.sendResponseHeaders(http, b.length);
        OutputStream os = ex.getResponseBody();
        try {
            os.write(b);
        } finally {
            os.close();
        }
    }

    private static Map<String, Object> cuerpo(HttpExchange ex) throws IOException {
        InputStream in = ex.getRequestBody();
        ByteArrayOutputStream buf = new ByteArrayOutputStream();
        byte[] trozo = new byte[4096];
        int n;
        while ((n = in.read(trozo)) > 0) {
            buf.write(trozo, 0, n);
            if (buf.size() > 64 * 1024) {
                throw new PuenteError(413, "TOO_LARGE", "Peticion demasiado grande.");
            }
        }
        return Json.leerObjeto(new String(buf.toByteArray(), StandardCharsets.UTF_8));
    }

    private static Map<String, String> consulta(HttpExchange ex) {
        Map<String, String> m = new LinkedHashMap<String, String>();
        String q = ex.getRequestURI().getRawQuery();
        if (q == null) {
            return m;
        }
        for (String par : q.split("&")) {
            int i = par.indexOf('=');
            if (i > 0) {
                m.put(decodificar(par.substring(0, i)), decodificar(par.substring(i + 1)));
            }
        }
        return m;
    }

    private static String valor(Map<String, String> m, String k, String porOmision) {
        String v = m.get(k);
        return v == null ? porOmision : v;
    }

    private static String decodificar(String s) {
        try {
            return URLDecoder.decode(s, "UTF-8");
        } catch (Exception e) {
            return s;
        }
    }
}
