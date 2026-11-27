package mx.novacore.puente;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * JSON mínimo (sin dependencias): objetos → LinkedHashMap, arreglos → ArrayList,
 * números → Long o Double, true/false → Boolean, null → null.
 */
public final class Json {

    private final String texto;
    private int i;

    private Json(String texto) {
        this.texto = texto;
    }

    public static Object leer(String texto) {
        if (texto == null || texto.trim().isEmpty()) {
            return null;
        }
        Json j = new Json(texto);
        j.espacios();
        Object valor = j.valor();
        j.espacios();
        if (j.i != texto.length()) {
            throw new IllegalArgumentException("JSON con texto de más en la posición " + j.i);
        }
        return valor;
    }

    @SuppressWarnings("unchecked")
    public static Map<String, Object> leerObjeto(String texto) {
        Object o = leer(texto);
        if (o == null) {
            return new LinkedHashMap<String, Object>();
        }
        if (!(o instanceof Map)) {
            throw new IllegalArgumentException("Se esperaba un objeto JSON.");
        }
        return (Map<String, Object>) o;
    }

    private void espacios() {
        while (i < texto.length() && Character.isWhitespace(texto.charAt(i))) {
            i++;
        }
    }

    private char ver() {
        if (i >= texto.length()) {
            throw new IllegalArgumentException("JSON incompleto.");
        }
        return texto.charAt(i);
    }

    private Object valor() {
        char c = ver();
        if (c == '{') {
            return objeto();
        }
        if (c == '[') {
            return arreglo();
        }
        if (c == '"') {
            return cadena();
        }
        if (texto.startsWith("true", i)) {
            i += 4;
            return Boolean.TRUE;
        }
        if (texto.startsWith("false", i)) {
            i += 5;
            return Boolean.FALSE;
        }
        if (texto.startsWith("null", i)) {
            i += 4;
            return null;
        }
        return numero();
    }

    private Map<String, Object> objeto() {
        Map<String, Object> mapa = new LinkedHashMap<String, Object>();
        i++;
        espacios();
        if (ver() == '}') {
            i++;
            return mapa;
        }
        while (true) {
            espacios();
            String clave = cadena();
            espacios();
            if (ver() != ':') {
                throw new IllegalArgumentException("Falta ':' en la posición " + i);
            }
            i++;
            espacios();
            mapa.put(clave, valor());
            espacios();
            char c = ver();
            i++;
            if (c == '}') {
                return mapa;
            }
            if (c != ',') {
                throw new IllegalArgumentException("Falta ',' en la posición " + i);
            }
        }
    }

    private List<Object> arreglo() {
        List<Object> lista = new ArrayList<Object>();
        i++;
        espacios();
        if (ver() == ']') {
            i++;
            return lista;
        }
        while (true) {
            espacios();
            lista.add(valor());
            espacios();
            char c = ver();
            i++;
            if (c == ']') {
                return lista;
            }
            if (c != ',') {
                throw new IllegalArgumentException("Falta ',' en la posición " + i);
            }
        }
    }

    private String cadena() {
        if (ver() != '"') {
            throw new IllegalArgumentException("Se esperaba texto en la posición " + i);
        }
        i++;
        StringBuilder sb = new StringBuilder();
        while (true) {
            char c = ver();
            i++;
            if (c == '"') {
                return sb.toString();
            }
            if (c != '\\') {
                sb.append(c);
                continue;
            }
            char e = ver();
            i++;
            switch (e) {
                case '"': sb.append('"'); break;
                case '\\': sb.append('\\'); break;
                case '/': sb.append('/'); break;
                case 'b': sb.append('\b'); break;
                case 'f': sb.append('\f'); break;
                case 'n': sb.append('\n'); break;
                case 'r': sb.append('\r'); break;
                case 't': sb.append('\t'); break;
                case 'u':
                    sb.append((char) Integer.parseInt(texto.substring(i, i + 4), 16));
                    i += 4;
                    break;
                default:
                    throw new IllegalArgumentException("Escape inválido en la posición " + i);
            }
        }
    }

    private Object numero() {
        int inicio = i;
        while (i < texto.length() && "+-0123456789.eE".indexOf(texto.charAt(i)) >= 0) {
            i++;
        }
        String n = texto.substring(inicio, i);
        if (n.isEmpty()) {
            throw new IllegalArgumentException("Valor inválido en la posición " + inicio);
        }
        if (n.indexOf('.') < 0 && n.indexOf('e') < 0 && n.indexOf('E') < 0) {
            return Long.valueOf(n);
        }
        return Double.valueOf(n);
    }

    // ------------------------------------------------------------ escribir

    public static String escribir(Object valor) {
        StringBuilder sb = new StringBuilder();
        escribir(sb, valor);
        return sb.toString();
    }

    @SuppressWarnings("unchecked")
    private static void escribir(StringBuilder sb, Object valor) {
        if (valor == null) {
            sb.append("null");
        } else if (valor instanceof String) {
            texto(sb, (String) valor);
        } else if (valor instanceof Boolean) {
            sb.append(valor.toString());
        } else if (valor instanceof Number) {
            if (valor instanceof Double || valor instanceof Float) {
                sb.append(new BigDecimal(valor.toString()).toPlainString());
            } else {
                sb.append(valor.toString());
            }
        } else if (valor instanceof Map) {
            sb.append('{');
            Iterator<Map.Entry<String, Object>> it = ((Map<String, Object>) valor).entrySet().iterator();
            boolean primero = true;
            while (it.hasNext()) {
                Map.Entry<String, Object> e = it.next();
                if (!primero) {
                    sb.append(',');
                }
                primero = false;
                texto(sb, e.getKey());
                sb.append(':');
                escribir(sb, e.getValue());
            }
            sb.append('}');
        } else if (valor instanceof List) {
            sb.append('[');
            boolean primero = true;
            for (Object o : (List<Object>) valor) {
                if (!primero) {
                    sb.append(',');
                }
                primero = false;
                escribir(sb, o);
            }
            sb.append(']');
        } else {
            texto(sb, valor.toString());
        }
    }

    private static void texto(StringBuilder sb, String s) {
        sb.append('"');
        for (int k = 0; k < s.length(); k++) {
            char c = s.charAt(k);
            switch (c) {
                case '"': sb.append("\\\""); break;
                case '\\': sb.append("\\\\"); break;
                case '\n': sb.append("\\n"); break;
                case '\r': sb.append("\\r"); break;
                case '\t': sb.append("\\t"); break;
                default:
                    if (c < 0x20) {
                        sb.append(String.format("\\u%04x", (int) c));
                    } else {
                        sb.append(c);
                    }
            }
        }
        sb.append('"');
    }

    // ------------------------------------------------------------ lectura cómoda

    public static String texto(Map<String, Object> m, String clave) {
        Object o = m.get(clave);
        return o == null ? null : o.toString().trim();
    }

    public static long entero(Map<String, Object> m, String clave, long porOmision) {
        Object o = m.get(clave);
        if (o instanceof Number) {
            return ((Number) o).longValue();
        }
        if (o instanceof String) {
            try {
                return Long.parseLong(((String) o).trim());
            } catch (NumberFormatException e) {
                return porOmision;
            }
        }
        return porOmision;
    }
}
