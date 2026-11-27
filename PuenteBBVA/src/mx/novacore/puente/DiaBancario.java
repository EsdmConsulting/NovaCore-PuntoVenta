package mx.novacore.puente;

import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;

/**
 * Regla de BBVA: la CANCELACIÓN de una venta se puede hacer antes de la
 * 1:00 a.m. (hora del centro de México) del día siguiente; después, solo
 * DEVOLUCIÓN.
 *
 * El "día bancario" de un instante es la fecha en CDMX restándole 1 hora.
 * Se usa UTC-6 fijo (CDMX no tiene horario de verano desde 2022) para no
 * depender de la versión de zonas horarias del Java instalado en la caja.
 */
public final class DiaBancario {

    private static final ZoneOffset CDMX = ZoneOffset.ofHours(-6);

    private DiaBancario() {
    }

    public static LocalDate de(long epochMillis) {
        return Instant.ofEpochMilli(epochMillis).minusSeconds(3600).atOffset(CDMX).toLocalDate();
    }

    /** true = todavía se puede CANCELAR (mismo día bancario). */
    public static boolean sePuedeCancelar(long cobroMillis, long ahoraMillis) {
        return de(cobroMillis).equals(de(ahoraMillis));
    }
}
