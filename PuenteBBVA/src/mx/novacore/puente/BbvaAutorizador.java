package mx.novacore.puente;

import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;
import java.util.Properties;

/**
 * Cobro real con el TOTAL POS SDK de BBVA (E-Global), versión Java.
 *
 * Se usa por REFLEXIÓN para que el puente compile y funcione sin el .jar
 * de BBVA: cuando llegue el kit, se copia el .jar a lib/ y se pone el
 * paquete de sus clases en puente.properties (bbva.paquete). No hay que
 * volver a compilar el puente.
 *
 * Clases y métodos según "Especificaciones TOTAL POS SDK" (Configuracion,
 * Interfaz, Peticion, Respuesta, Tarjeta, OPERACION, PARAMETRO_OPERACION,
 * MONEDA, PinPadKeysException, PeticionException).
 *
 * PENDIENTE DE PROBAR CON EL KIT REAL: nombres exactos de paquete y de
 * los getters de Respuesta.
 */
public final class BbvaAutorizador implements Autorizador {

    private final Config config;
    private final String[] paquetes;
    private volatile boolean listo;
    private volatile String estado = "Sin inicializar.";
    private volatile Object peticionEnCurso;
    private boolean configurado;

    public BbvaAutorizador(Config config) {
        this.config = config;
        this.paquetes = config.texto("bbva.paquete", "").split("\\s*,\\s*");
    }

    public String procesador() {
        return "BBVA_TOTALPOS";
    }

    public boolean listo() {
        return listo;
    }

    public String estado() {
        return estado;
    }

    // ============================================================ inicializar

    public synchronized void inicializar() throws Exception {
        try {
            if (!configurado) {
                configurar();
                configurado = true;
            }

            // Bandera de carga de llaves que deja el host en pinpad.config.
            if (banderaCargaLlaves()) {
                Registro.info("pinpad.config pide carga de llaves.");
                cargarLlavesInterno();
            }

            try {
                invocar(interfaz(), "inicializar");
            } catch (Exception e) {
                if (!esExcepcion(e, "PinPadKeysException")) {
                    throw e;
                }
                Registro.info("El lector requiere carga de llaves.");
                cargarLlavesInterno();
                invocar(interfaz(), "inicializar");
            }

            listo = true;
            estado = "Listo.";
        } catch (Exception e) {
            listo = false;
            estado = "El lector BBVA no esta listo: " + Motor.mensaje(e);
            throw e;
        }
    }

    private void configurar() throws Exception {
        Object conf = clase("Configuracion").getDeclaredConstructor().newInstance();

        String ambiente = config.texto("bbva.ambiente", "TESTING").toUpperCase();
        String base;
        String token;
        String telecarga;
        if (ambiente.equals("PRODUCCION")) {
            base = "https://www.totalpos.eglobal.com.mx";
            token = base + "/totalpos/ws/autenticacion/oauth/token";
            telecarga = base + "/telecargas/descargas";
        } else if (ambiente.equals("SIMULADOR")) {
            base = config.texto("bbva.simulador_url", "http://localhost:8080");
            token = base + "/oauth/token";
            telecarga = base + "/descargas";
        } else {
            base = "https://totalpostest.egl-cloud.com";
            token = base + "/totalpos/ws/autenticacion/oauth/token";
            telecarga = base + "/telecargas/descargas";
        }

        invocar(conf, "setLogs", config.bandera("bbva.logs", true));
        String conexion = config.texto("bbva.pinpad.conexion", "");
        if (!conexion.isEmpty()) {
            invocar(conf, "setPinpadConexion", conexion);
        }
        invocar(conf, "setPinpadTimeOut", config.texto("bbva.pinpad.timeout", "40"));
        if (conexion.matches("\\d+\\.\\d+\\.\\d+\\.\\d+")) {
            invocar(conf, "setPinpadPuerto", config.texto("bbva.pinpad.puerto", "1087"));
        }
        invocar(conf, "setPinpadMensaje", config.texto("bbva.pinpad.mensaje", "GRUPO LUIS FER"));
        invocar(conf, "setPinpadContactless", config.bandera("bbva.pinpad.contactless", true));
        invocar(conf, "setClaveBines", config.texto("bbva.clave_bines", ""));
        invocar(conf, "setHostUrl", config.texto("bbva.host_url", base + "/totalpos/ws/autorizaciones/transacciones"));
        invocar(conf, "setBinesUrl", config.texto("bbva.bines_url", base + "/totalpos/ws/recursos/bines/concentrado/actualizaciones"));
        invocar(conf, "setTokenUrl", config.texto("bbva.token_url", token));
        invocar(conf, "setTeleCargaUrl", config.texto("bbva.telecarga_url", telecarga));
        invocar(conf, "setHostTimeOut", config.texto("bbva.host_timeout", "40"));
        invocar(conf, "setFuncionalidadGaranti", config.bandera("bbva.garanti", false));
        invocar(conf, "setFuncionalidadMoto", false);
        invocar(conf, "setComercioAfiliacion", obligatorio(config.afiliacion(), "bbva.afiliacion"));
        invocar(conf, "setComercioTerminal", obligatorio(config.numeroTerminal(), "bbva.terminal"));
        invocar(conf, "setComercioMac", obligatorio(config.mac(), "bbva.mac"));
        invocar(conf, "setIdAplicacion", obligatorio(config.texto("bbva.id_aplicacion", ""), "bbva.id_aplicacion"));
        invocar(conf, "setClaveSecreta", obligatorio(config.texto("bbva.clave_secreta", ""), "bbva.clave_secreta"));

        invocar(interfaz(), "setConfiguracion", conf);
        Registro.info("SDK BBVA configurado (" + ambiente + ", afiliacion " + config.afiliacion()
                + ", terminal " + config.numeroTerminal() + ").");
    }

    private static String obligatorio(String valor, String clave) {
        if (valor == null || valor.trim().isEmpty()) {
            throw new IllegalStateException("Falta " + clave + " en puente.properties.");
        }
        return valor.trim();
    }

    private boolean banderaCargaLlaves() {
        File f = new File(config.texto("bbva.pinpad_config", "pinpad.config"));
        if (!f.isFile()) {
            return false;
        }
        Properties p = new Properties();
        try {
            InputStream in = new FileInputStream(f);
            try {
                p.load(in);
            } finally {
                in.close();
            }
        } catch (Exception e) {
            return false;
        }
        String v = p.getProperty("pinpad.carga.llaves");
        if (v == null) {
            v = p.getProperty("Pinpad.carga.llaves");
        }
        return v != null && v.trim().equals("1");
    }

    // ============================================================ venta

    public ResultadoBanco vender(Cobro cobro, Autorizador.Avance avance) throws Exception {
        Object p;
        Object tarjeta;

        try {
            Map<Object, Object> parametros = new HashMap<Object, Object>();
            parametros.put(enumValor("PARAMETRO_OPERACION", "IMPORTE"), Cobro.importeTexto(cobro.centavos));
            parametros.put(enumValor("PARAMETRO_OPERACION", "REFERENCIA_COMERCIO"), Motor.recortar(cobro.referencia, 45));

            p = peticion(cobro.operador);
            invocar(p, "setOperacion", enumValor("OPERACION", "VENTA"), parametros);

            avance.etapa("ESPERANDO_TARJETA");
            peticionEnCurso = p;
            tarjeta = invocar(p, "leerTarjeta");
            peticionEnCurso = null;

            if (avance.cancelado()) {
                cerrar(p);
                return ResultadoBanco.cancelado();
            }
            avance.etapa("TARJETA_DETECTADA");
        } catch (Exception e) {
            peticionEnCurso = null;
            if (avance.cancelado()) {
                return ResultadoBanco.cancelado();
            }
            if (esExcepcion(e, "PinPadKeysException")) {
                return llavesYAvisar();
            }
            // Todavía no se mandó nada al banco: no hubo cargo.
            return ResultadoBanco.conError("El lector no pudo leer la tarjeta: " + Motor.mensaje(e));
        }

        avance.etapa("PROCESANDO");

        Object respuesta;
        try {
            respuesta = invocar(p, "autorizar");
        } catch (Exception e) {
            if (esExcepcion(e, "PinPadKeysException")) {
                return llavesYAvisar();
            }
            // Se mandó al banco y no hubo respuesta: el SDK guarda el reverso.
            Registro.aviso("Sin respuesta del banco en " + cobro.referencia + ": " + Motor.mensaje(e));
            ResultadoBanco r = new ResultadoBanco();
            r.sinRespuesta = true;
            return r;
        }

        ResultadoBanco r = leerRespuesta(respuesta, "VENTA");
        completarConTarjeta(r, tarjeta);
        return r;
    }

    public void cancelarLectura() {
        Object p = peticionEnCurso;
        if (p != null) {
            cerrar(p);
        }
    }

    private void cerrar(Object p) {
        try {
            invocar(p, "sincronizacionFinal");
        } catch (Exception e) {
            Registro.aviso("sincronizacionFinal: " + Motor.mensaje(e));
        }
    }

    private ResultadoBanco llavesYAvisar() {
        try {
            cargarLlavesInterno();
            invocar(interfaz(), "inicializar");
            return ResultadoBanco.conError("El lector necesitaba carga de llaves y ya se hizo. No se cobro: cobre de nuevo.");
        } catch (Exception e) {
            listo = false;
            estado = "Falta la carga de llaves del lector: " + Motor.mensaje(e);
            return ResultadoBanco.conError(estado);
        }
    }

    // ============================================================ cancelación / devolución

    public ResultadoBanco reversar(Cobro original, boolean esCancelacion, long centavos, Autorizador.Avance avance)
            throws Exception {

        String refFinanciera = original.banco == null ? null : (String) original.banco.get("financial_reference");
        if (refFinanciera == null || refFinanciera.trim().isEmpty()) {
            return ResultadoBanco.conError("El cobro original no tiene referencia financiera; no se puede regresar desde el lector.");
        }

        Object p;
        Object tarjeta = null;
        try {
            Map<Object, Object> parametros = new HashMap<Object, Object>();
            parametros.put(enumValor("PARAMETRO_OPERACION", "REFERENCIA_COMERCIO"), Motor.recortar(original.referencia, 45));
            parametros.put(enumValor("PARAMETRO_OPERACION", "REFERENCIA_FINANCIERA"), refFinanciera.trim());
            if (!esCancelacion) {
                parametros.put(enumValor("PARAMETRO_OPERACION", "IMPORTE"), Cobro.importeTexto(centavos));
            }

            p = peticion(original.operador);
            invocar(p, "setOperacion",
                    enumValor("OPERACION", esCancelacion ? "CANCELACION_VENTA" : "DEVOLUCION"), parametros);

            if (!esCancelacion) {
                // La devolución pide la tarjeta del cliente.
                avance.etapa("ESPERANDO_TARJETA");
                peticionEnCurso = p;
                tarjeta = invocar(p, "leerTarjeta");
                peticionEnCurso = null;
                if (avance.cancelado()) {
                    cerrar(p);
                    return ResultadoBanco.cancelado();
                }
            }
        } catch (Exception e) {
            peticionEnCurso = null;
            if (avance.cancelado()) {
                return ResultadoBanco.cancelado();
            }
            if (esExcepcion(e, "PinPadKeysException")) {
                return llavesYAvisar();
            }
            return ResultadoBanco.conError("No se pudo preparar la operacion: " + Motor.mensaje(e));
        }

        avance.etapa("PROCESANDO");

        try {
            ResultadoBanco r = leerRespuesta(invocar(p, "autorizar"), esCancelacion ? "CANCELACION" : "DEVOLUCION");
            if (tarjeta != null) {
                completarConTarjeta(r, tarjeta);
            }
            return r;
        } catch (Exception e) {
            Registro.aviso("Sin respuesta del banco al regresar " + original.referencia + ": " + Motor.mensaje(e));
            ResultadoBanco r = new ResultadoBanco();
            r.sinRespuesta = true;
            return r;
        }
    }

    // ============================================================ llaves

    public synchronized ResultadoBanco cargarLlaves() throws Exception {
        if (!configurado) {
            configurar();
            configurado = true;
        }
        return cargarLlavesInterno();
    }

    private ResultadoBanco cargarLlavesInterno() throws Exception {
        Object p = peticion(null);
        invocar(p, "setOperacion", enumValor("OPERACION", "CARGA_LLAVES"), null);
        ResultadoBanco r = leerRespuesta(invocar(p, "autorizar"), "CARGA_LLAVES");
        Registro.info("Carga de llaves: codigo " + r.codigoRespuesta);
        return r;
    }

    // ============================================================ SDK por reflexión

    private Object peticion(String operador) throws Exception {
        Object p = clase("Peticion").getDeclaredConstructor().newInstance();
        invocar(p, "setAfiliacion", config.afiliacion(), enumValor("MONEDA", "PESOS"));
        invocar(p, "setTerminal", config.numeroTerminal(), config.mac());
        invocar(p, "setOperador", Motor.recortar(operador == null || operador.isEmpty()
                ? config.texto("bbva.operador", "CAJA") : operador, 30));
        invocar(p, "setFecha", new SimpleDateFormat("yyyyMMddHHmmss").format(new Date()));
        invocar(p, "setAmex", false);
        return p;
    }

    private Object interfaz() throws Exception {
        Method m = clase("Interfaz").getMethod("getInstance");
        return m.invoke(null);
    }

    private ResultadoBanco leerRespuesta(Object resp, String operacion) {
        ResultadoBanco r = new ResultadoBanco();
        r.codigoRespuesta = leer(resp, "CodigoRespuesta", "codigoRespuesta");
        r.aprobado = "00".equals(r.codigoRespuesta);
        r.leyenda = leer(resp, "Leyenda", "leyenda");
        r.autorizacion = leer(resp, "Autorizacion", "autorizacion");
        r.referenciaFinanciera = leer(resp, "ReferenciaFinanciera", "referenciaFinanciera");
        r.secuencia = leer(resp, "SecuenciaTransaccion", "secuenciaTransaccion");
        String numero = leer(resp, "NumeroTarjeta", "numeroTarjeta");
        r.ultimos4 = ResultadoBanco.ultimos4(numero);
        r.marca = ResultadoBanco.marcaPorNumero(numero);
        r.emisor = leer(resp, "EmisorTarjeta", "emisorTarjeta");
        r.producto = leer(resp, "ProductoTarjeta", "productoTarjeta");
        r.tarjetahabiente = leer(resp, "Tarjetahabiente", "tarjetahabiente");
        r.modoLectura = leer(resp, "ModoLectura", "modoLectura");
        r.aid = leer(resp, "IdAplicacionTarjeta", "idAplicacionTarjeta");
        r.aplicacion = leer(resp, "AplicacionTarjeta", "aplicacionTarjeta");
        r.arqc = leer(resp, "CriptogramaTarjeta", "criptogramaTarjeta");
        r.firma = firma(leer(resp, "Firma", "firma"));
        r.fechaHora = leer(resp, "FechaHora", "fechaHora");
        r.afiliacion = leer(resp, "Afiliacion", "afiliacion");
        r.numeroTerminal = leer(resp, "NumeroTerminal", "numeroTerminal");
        r.nombreTransaccion = leer(resp, "NombreTransaccion", "nombreTransaccion");
        String moneda = leer(resp, "Moneda", "moneda");
        r.moneda = moneda != null && moneda.toUpperCase().startsWith("DOL") ? "USD" : "MXN";
        if (r.nombreTransaccion == null) {
            r.nombreTransaccion = operacion;
        }
        if (r.afiliacion == null) {
            r.afiliacion = config.afiliacion();
        }
        if (r.numeroTerminal == null) {
            r.numeroTerminal = config.numeroTerminal();
        }
        return r;
    }

    /** Lo que solo trae la tarjeta leída (crédito/débito, emisor, nombre). */
    private void completarConTarjeta(ResultadoBanco r, Object tarjeta) {
        if (tarjeta == null) {
            return;
        }
        String producto = leer(tarjeta, "Producto", "producto");
        if (producto != null) {
            String pr = producto.trim().toLowerCase();
            if (pr.startsWith("c")) {
                r.producto = "CREDITO";
            } else if (pr.startsWith("d")) {
                r.producto = "DEBITO";
            }
        }
        if (r.tarjetahabiente == null) {
            r.tarjetahabiente = leer(tarjeta, "Tarjetahabiente", "tarjetahabiente");
        }
        String pan = leer(tarjeta, "Pan", "pan");
        if (r.ultimos4 == null) {
            r.ultimos4 = ResultadoBanco.ultimos4(pan);
        }
        if (r.marca == null) {
            r.marca = ResultadoBanco.marcaPorNumero(pan);
        }
        if (r.emisor == null) {
            String emisor = leer(tarjeta, "Emisor", "emisor");
            r.emisor = "12".equals(emisor) ? "BBVA" : emisor;
        }
    }

    private static String firma(String valor) {
        if (valor == null) {
            return null;
        }
        String v = valor.toUpperCase().replace(' ', '_');
        if (v.contains("AUTOGRAFA") || v.startsWith("01")) {
            return "AUTOGRAFA";
        }
        if (v.contains("ELECTRONICA") || v.contains("QPS") || v.startsWith("02")) {
            return "ELECTRONICA";
        }
        if (v.contains("SIN") || v.startsWith("00")) {
            return "SIN_FIRMA";
        }
        return v;
    }

    /** getXxx() o, si la Respuesta es un Map, get("xxx"). */
    @SuppressWarnings("rawtypes")
    private static String leer(Object o, String getter, String llave) {
        if (o == null) {
            return null;
        }
        Object v = null;
        for (String prefijo : new String[]{"get", "is"}) {
            try {
                Method m = o.getClass().getMethod(prefijo + getter);
                v = m.invoke(o);
                break;
            } catch (NoSuchMethodException e) {
                // siguiente
            } catch (Exception e) {
                return null;
            }
        }
        if (v == null && o instanceof Map) {
            v = ((Map) o).get(llave);
        }
        if (v == null) {
            return null;
        }
        if (v instanceof Enum) {
            return ((Enum<?>) v).name();
        }
        String s = v.toString().trim();
        return s.isEmpty() ? null : s;
    }

    private Class<?> clase(String nombre) throws ClassNotFoundException {
        String directa = config.texto("bbva.clase." + nombre, null);
        if (directa != null) {
            return Class.forName(directa);
        }
        for (String paquete : paquetes) {
            if (paquete.isEmpty()) {
                continue;
            }
            try {
                return Class.forName(paquete + "." + nombre);
            } catch (ClassNotFoundException e) {
                // siguiente paquete
            }
        }
        throw new ClassNotFoundException("No se encontro la clase " + nombre
                + " del SDK de BBVA. Revise que el .jar este en lib/ y bbva.paquete en puente.properties.");
    }

    @SuppressWarnings({"unchecked", "rawtypes"})
    private Object enumValor(String tipo, String valor) throws Exception {
        Class<?> c;
        try {
            c = clase(tipo);
        } catch (ClassNotFoundException e) {
            c = null;
            // Puede venir anidado (Peticion.OPERACION, etc.).
            for (String contenedor : new String[]{"Peticion", "Interfaz", "Configuracion", "Respuesta"}) {
                try {
                    for (Class<?> interna : clase(contenedor).getClasses()) {
                        if (interna.getSimpleName().equals(tipo)) {
                            c = interna;
                            break;
                        }
                    }
                } catch (ClassNotFoundException ignorado) {
                    // siguiente
                }
                if (c != null) {
                    break;
                }
            }
            if (c == null) {
                throw e;
            }
        }
        return Enum.valueOf((Class) c, valor);
    }

    private static Object invocar(Object objeto, String metodo, Object... args) throws Exception {
        Method elegido = null;
        for (Method m : objeto.getClass().getMethods()) {
            if (!m.getName().equals(metodo) || m.getParameterTypes().length != args.length) {
                continue;
            }
            if (compatibles(m.getParameterTypes(), args)) {
                elegido = m;
                break;
            }
        }
        if (elegido == null) {
            throw new NoSuchMethodException(objeto.getClass().getSimpleName() + "." + metodo
                    + " con " + args.length + " parametros no existe en el SDK.");
        }
        try {
            return elegido.invoke(Modifier.isStatic(elegido.getModifiers()) ? null : objeto, args);
        } catch (InvocationTargetException e) {
            Throwable causa = e.getCause();
            if (causa instanceof Exception) {
                throw (Exception) causa;
            }
            throw e;
        }
    }

    private static boolean compatibles(Class<?>[] tipos, Object[] args) {
        for (int k = 0; k < tipos.length; k++) {
            Object a = args[k];
            Class<?> t = tipos[k];
            if (a == null) {
                if (t.isPrimitive()) {
                    return false;
                }
                continue;
            }
            if (t.isPrimitive()) {
                if ((t == boolean.class && a instanceof Boolean) || (t == int.class && a instanceof Integer)
                        || (t == long.class && a instanceof Long)) {
                    continue;
                }
                return false;
            }
            if (!t.isInstance(a)) {
                return false;
            }
        }
        return true;
    }

    private static boolean esExcepcion(Throwable e, String nombreSimple) {
        Throwable t = e;
        while (t != null) {
            if (t.getClass().getSimpleName().equals(nombreSimple)) {
                return true;
            }
            t = t.getCause();
        }
        return false;
    }
}
