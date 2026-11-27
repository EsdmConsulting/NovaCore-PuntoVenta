package mx.novacore.puente;

import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStreamWriter;
import java.io.Writer;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Bitácora del puente (datos/puente-AAAAMMDD.log). NUNCA se escribe aquí el
 * número de tarjeta, el NIP ni las claves.
 */
public final class Registro {

    private static File carpeta;

    private Registro() {
    }

    public static void configurar(File datos) {
        carpeta = datos;
    }

    public static void info(String texto) {
        escribir("INFO ", texto);
    }

    public static void aviso(String texto) {
        escribir("AVISO", texto);
    }

    private static synchronized void escribir(String nivel, String texto) {
        String linea = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss").format(new Date()) + " " + nivel + " " + texto;
        System.out.println(linea);
        if (carpeta == null) {
            return;
        }
        File f = new File(carpeta, "puente-" + new SimpleDateFormat("yyyyMMdd").format(new Date()) + ".log");
        try {
            Writer w = new OutputStreamWriter(new FileOutputStream(f, true), StandardCharsets.UTF_8);
            try {
                w.write(linea);
                w.write(System.lineSeparator());
            } finally {
                w.close();
            }
        } catch (Exception ignorado) {
            // la bitácora nunca detiene un cobro
        }
    }
}
