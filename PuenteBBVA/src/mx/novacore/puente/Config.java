package mx.novacore.puente;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.Reader;
import java.io.Writer;
import java.net.NetworkInterface;
import java.nio.charset.StandardCharsets;
import java.security.SecureRandom;
import java.util.Enumeration;
import java.util.Properties;

/**
 * Configuración del puente: puente.properties junto al programa.
 * Ver puente.properties.ejemplo.
 */
public final class Config {

    public final File carpeta;
    public final File datos;
    private final Properties p = new Properties();

    public Config(File carpeta) throws IOException {
        this.carpeta = carpeta;
        File archivo = new File(carpeta, "puente.properties");
        if (archivo.isFile()) {
            Reader r = new InputStreamReader(new FileInputStream(archivo), StandardCharsets.UTF_8);
            try {
                p.load(r);
            } finally {
                r.close();
            }
        }
        this.datos = new File(carpeta, texto("puente.datos", "datos"));
        if (!datos.isDirectory() && !datos.mkdirs()) {
            throw new IOException("No se pudo crear la carpeta de datos: " + datos);
        }
    }

    public String texto(String clave, String porOmision) {
        String v = p.getProperty(clave);
        return v == null || v.trim().isEmpty() ? porOmision : v.trim();
    }

    public int entero(String clave, int porOmision) {
        try {
            return Integer.parseInt(texto(clave, String.valueOf(porOmision)));
        } catch (NumberFormatException e) {
            return porOmision;
        }
    }

    public boolean bandera(String clave, boolean porOmision) {
        String v = texto(clave, porOmision ? "true" : "false").toLowerCase();
        return v.equals("true") || v.equals("1") || v.equals("si") || v.equals("sí");
    }

    // ------------------------------------------------------------ del puente

    /** SIMULADOR o BBVA. */
    public String modo() {
        return texto("puente.modo", "SIMULADOR").toUpperCase();
    }

    public String host() {
        return texto("puente.host", "127.0.0.1");
    }

    public int puerto() {
        return entero("puente.puerto", 8091);
    }

    /** Código con el que el POS identifica al lector (Configuración › Terminales de cobro). */
    public String codigoTerminal() {
        return texto("terminal.codigo", "BBVA").toUpperCase();
    }

    public String nombreTerminal() {
        return texto("terminal.nombre", "Lector BBVA");
    }

    /**
     * Clave de acceso (Bearer) que el POS manda en cada petición. Si no se
     * configura, se genera una la primera vez y se guarda en datos/clave.txt.
     */
    public String clave() throws IOException {
        String c = texto("puente.clave", null);
        if (c != null) {
            return c;
        }
        File f = new File(datos, "clave.txt");
        if (f.isFile()) {
            byte[] b = java.nio.file.Files.readAllBytes(f.toPath());
            String guardada = new String(b, StandardCharsets.UTF_8).trim();
            if (!guardada.isEmpty()) {
                return guardada;
            }
        }
        SecureRandom r = new SecureRandom();
        StringBuilder sb = new StringBuilder();
        String letras = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for (int k = 0; k < 24; k++) {
            sb.append(letras.charAt(r.nextInt(letras.length())));
        }
        Writer w = new OutputStreamWriter(new FileOutputStream(f), StandardCharsets.UTF_8);
        try {
            w.write(sb.toString());
        } finally {
            w.close();
        }
        return sb.toString();
    }

    // ------------------------------------------------------------ BBVA

    public String afiliacion() {
        return texto("bbva.afiliacion", "");
    }

    public String numeroTerminal() {
        return texto("bbva.terminal", "");
    }

    /** MAC registrada en el Gestor de Terminales de BBVA (si no se pone, la de la tarjeta de red). */
    public String mac() {
        String m = texto("bbva.mac", null);
        return m != null ? m.toUpperCase() : macDelEquipo();
    }

    public static String macDelEquipo() {
        try {
            Enumeration<NetworkInterface> lista = NetworkInterface.getNetworkInterfaces();
            while (lista != null && lista.hasMoreElements()) {
                NetworkInterface n = lista.nextElement();
                if (n.isLoopback() || n.isVirtual() || !n.isUp()) {
                    continue;
                }
                byte[] b = n.getHardwareAddress();
                if (b == null || b.length != 6) {
                    continue;
                }
                StringBuilder sb = new StringBuilder();
                for (int k = 0; k < b.length; k++) {
                    if (k > 0) {
                        sb.append('-');
                    }
                    sb.append(String.format("%02X", b[k]));
                }
                return sb.toString();
            }
        } catch (Exception ignorado) {
            // sin red: se queda vacío
        }
        return "";
    }
}
