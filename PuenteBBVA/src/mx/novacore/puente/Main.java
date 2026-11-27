package mx.novacore.puente;

import java.io.File;

/**
 * Puente NovaCore ↔ lector de tarjetas BBVA (TOTAL POS SDK).
 *
 * Corre en la misma computadora que la caja. El POS le manda el importe por
 * la red local (http://127.0.0.1:8091) y el puente lo cobra con el lector,
 * con el SDK de BBVA o con el simulador.
 *
 * Uso: java -cp "puente-bbva.jar:lib/*" mx.novacore.puente.Main [carpeta]
 *      (en Windows el separador es ";")
 */
public final class Main {

    public static final String VERSION = "1.0.0";

    private Main() {
    }

    public static void main(String[] args) throws Exception {
        File carpeta = new File(args.length > 0 ? args[0] : ".").getAbsoluteFile();
        Config config = new Config(carpeta);
        Registro.configurar(config.datos);

        Autorizador autorizador = config.modo().equals("BBVA")
                ? new BbvaAutorizador(config)
                : new SimuladorAutorizador(config);

        Motor motor = new Motor(autorizador, config);
        final Servidor servidor = new Servidor(motor, config);
        servidor.iniciar();
        motor.arrancar();

        Registro.info("Puente BBVA " + VERSION + " en http://" + config.host() + ":" + config.puerto()
                + " — modo " + autorizador.procesador() + " — terminal " + config.codigoTerminal()
                + " — " + autorizador.estado());
        Registro.info("Clave de acceso para el POS: datos/clave.txt (o puente.clave en puente.properties).");

        Runtime.getRuntime().addShutdownHook(new Thread(new Runnable() {
            public void run() {
                servidor.detener();
            }
        }));

        Thread.currentThread().join();
    }
}
