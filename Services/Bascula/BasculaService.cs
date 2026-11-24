using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Configuration;

namespace NovaCoreESDM.Services.Bascula;

// ============================================================
// BÁSCULA ELECTRÓNICA POR PUERTO SERIAL (USB → COM)
// ============================================================
//
// La Noval N40 se conecta por USB con un chip CH341, que Windows
// presenta como un puerto COM. La báscula manda el peso como texto
// (p. ej. "ST,GS,+  1.234kg", "  1.234 kg", "001234"…).
//
// Este lector:
//   • abre el puerto y escucha lo que manda la báscula;
//   • si la báscula no manda nada sola, le pide el peso con el
//     comando configurado;
//   • convierte cada lectura a KG y decide si el peso es estable.
//
// Los eventos llegan en un hilo de fondo: quien los use debe pasar
// a la UI con Dispatcher.UIThread.Post.
// ============================================================


/// <summary>
/// Una lectura de la báscula, ya convertida a kilogramos.
/// </summary>
public sealed record LecturaBascula(
    decimal PesoKg,
    bool Estable,
    string Crudo);


/// <summary>
/// Resultado de buscar la báscula en los puertos del equipo.
/// </summary>
public sealed record DeteccionBascula(
    string Puerto,
    int BaudRate,
    string Comando,
    LecturaBascula Muestra);


public sealed class BasculaLector : IDisposable
{
    // ============================================================
    // CONSTANTES
    // ============================================================

    /// <summary>
    /// Velocidades que se prueban al detectar (la más común primero).
    /// </summary>
    public static readonly int[] VelocidadesComunes =
        { 9600, 4800, 2400, 19200, 1200 };

    /// <summary>
    /// Comandos para pedir el peso que usan la mayoría de las básculas.
    /// "" = no mandar nada (la báscula transmite sola).
    /// </summary>
    public static readonly string[] ComandosComunes =
        { "", "P", "W", "$", "\\x05", "R" };

    private const decimal ToleranciaEstableKg = 0.004m;
    private static readonly TimeSpan TiempoEstable = TimeSpan.FromMilliseconds(450);
    private static readonly TimeSpan IntervaloPedido = TimeSpan.FromMilliseconds(250);
    private const decimal LibraAKg = 0.45359237m;


    // ============================================================
    // ESTADO
    // ============================================================

    private readonly ScaleConfiguration _configuracion;

    private SerialPort? _puerto;
    private CancellationTokenSource? _cts;
    private Task? _tarea;

    private readonly StringBuilder _buffer = new();
    private readonly List<(DateTime Hora, decimal Peso)> _ultimas = new();

    private readonly string _comando;


    /// <summary>Cada vez que llega un peso válido.</summary>
    public event Action<LecturaBascula>? PesoLeido;

    /// <summary>Cada trama tal como la mandó la báscula (diagnóstico).</summary>
    public event Action<string>? TramaRecibida;

    /// <summary>El puerto falló o se desconectó.</summary>
    public event Action<string>? Fallo;

    public bool EstaAbierto => _puerto?.IsOpen == true;


    public BasculaLector(ScaleConfiguration configuracion)
    {
        _configuracion = configuracion;
        _comando = configuracion.Comando ?? string.Empty;
    }


    // ============================================================
    // PUERTOS DEL EQUIPO
    // ============================================================

    /// <summary>
    /// Puertos seriales disponibles. En macOS se quitan los que no
    /// son físicos (Bluetooth, consola) y se prefieren los /dev/cu.*.
    /// </summary>
    public static List<string> ListarPuertos()
    {
        try
        {
            var puertos = SerialPort.GetPortNames()
                .Where(p => !p.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase)
                         && !p.Contains("debug-console", StringComparison.OrdinalIgnoreCase)
                         && !p.Contains("wlan", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            if (!OperatingSystem.IsWindows())
            {
                // /dev/tty.X y /dev/cu.X son el mismo dispositivo:
                // nos quedamos con cu (no espera señal de módem).
                var cu = puertos.Where(p => p.StartsWith("/dev/cu.")).ToList();
                if (cu.Count > 0)
                    puertos = cu;
            }

            return puertos
                .OrderBy(p => p.Length)
                .ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }


    // ============================================================
    // ABRIR / CERRAR
    // ============================================================

    public bool Iniciar(out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(_configuracion.Puerto))
        {
            error = "No hay puerto de báscula configurado.";
            return false;
        }

        try
        {
            try
            {
                // Varias básculas con CH341 no transmiten si DTR/RTS están abajo.
                _puerto = CrearPuerto(conSenales: true);
                _puerto.Open();
            }
            catch (IOException)
            {
                // Adaptadores que no manejan DTR/RTS: abrir sin señales.
                CerrarPuerto();
                _puerto = CrearPuerto(conSenales: false);
                _puerto.Open();
            }

            _puerto.DiscardInBuffer();
        }
        catch (UnauthorizedAccessException)
        {
            error = $"El puerto {_configuracion.Puerto} está ocupado por otro programa.";
            CerrarPuerto();
            return false;
        }
        catch (Exception ex)
        {
            error = $"No fue posible abrir {_configuracion.Puerto}: {ex.Message}";
            CerrarPuerto();
            return false;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _tarea = Task.Run(() => CicloLectura(token), token);
        return true;
    }


    private SerialPort CrearPuerto(bool conSenales) =>
        new(
            _configuracion.Puerto,
            _configuracion.BaudRate > 0 ? _configuracion.BaudRate : 9600,
            Parity.None,
            8,
            StopBits.One)
        {
            Encoding = Encoding.ASCII,
            ReadTimeout = 200,
            WriteTimeout = 300,
            Handshake = Handshake.None,
            DtrEnable = conSenales,
            RtsEnable = conSenales
        };


    public void Detener()
    {
        try { _cts?.Cancel(); } catch { /* ya cancelado */ }

        try { _tarea?.Wait(600); } catch { /* cancelación */ }

        CerrarPuerto();

        _cts?.Dispose();
        _cts = null;
        _tarea = null;
    }


    private void CerrarPuerto()
    {
        try
        {
            if (_puerto?.IsOpen == true)
                _puerto.Close();
        }
        catch { /* el USB pudo desconectarse */ }

        _puerto?.Dispose();
        _puerto = null;
    }


    public void Dispose() => Detener();


    // ============================================================
    // CICLO DE LECTURA
    // ============================================================

    private void CicloLectura(CancellationToken token)
    {
        var ultimoPedido = DateTime.MinValue;
        var comando = DecodificarComando(_comando);

        while (!token.IsCancellationRequested)
        {
            var puerto = _puerto;
            if (puerto is null || !puerto.IsOpen)
            {
                Fallo?.Invoke("Se perdió la conexión con la báscula.");
                return;
            }

            try
            {
                var llegaron = puerto.BytesToRead > 0
                    ? puerto.ReadExisting()
                    : string.Empty;

                if (llegaron.Length > 0)
                    ProcesarDatos(llegaron);

                // Báscula "a pedido": pedirle el peso unas 4 veces por segundo.
                // (Las de envío continuo no llevan comando y no se les manda nada.)
                var ahora = DateTime.UtcNow;
                if (comando.Length > 0 && ahora - ultimoPedido >= IntervaloPedido)
                {
                    puerto.Write(comando, 0, comando.Length);
                    ultimoPedido = ahora;
                }
            }
            catch (TimeoutException)
            {
                // Sin datos: normal.
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Fallo?.Invoke($"La báscula dejó de responder: {ex.Message}");
                return;
            }

            try { Task.Delay(60, token).Wait(token); }
            catch (OperationCanceledException) { return; }
        }
    }


    /// <summary>
    /// Junta lo recibido, lo corta en tramas y procesa cada una.
    /// </summary>
    private void ProcesarDatos(string datos)
    {
        foreach (var c in datos)
        {
            // Fin de trama: CR, LF, STX, ETX.
            if (c is '\r' or '\n' or '\x02' or '\x03')
            {
                ProcesarTrama(_buffer.ToString());
                _buffer.Clear();
                continue;
            }

            _buffer.Append(c);
        }

        // Básculas que no mandan fin de trama: procesar si se acumuló algo
        // con forma de peso y ya no viene más.
        if (_buffer.Length > 64)
        {
            ProcesarTrama(_buffer.ToString());
            _buffer.Clear();
        }
    }


    private bool ProcesarTrama(string trama)
    {
        var limpia = new string(trama.Where(ch => ch >= ' ' && ch < 127).ToArray()).Trim();

        if (limpia.Length == 0)
            return false;

        TramaRecibida?.Invoke(limpia);

        if (!InterpretarPeso(limpia, _configuracion.DecimalesSinPunto,
                out var pesoKg, out var estabilidad))
            return false;

        var estable = CalcularEstabilidad(pesoKg, estabilidad);

        PesoLeido?.Invoke(new LecturaBascula(pesoKg, estable, limpia));
        return true;
    }


    // ============================================================
    // INTERPRETAR LA TRAMA
    // ============================================================

    private static readonly Regex NumeroRegex = new(
        @"(?<signo>[-+])?\s*(?<num>\d+(?:[.,]\d+)?)\s*(?<unidad>kg|lb|oz|g)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);


    /// <summary>
    /// Saca el peso de una trama. estabilidad: true = la báscula dice
    /// "estable" (ST), false = dice "moviéndose" (US), null = no dice.
    /// </summary>
    public static bool InterpretarPeso(
        string trama,
        int decimalesSinPunto,
        out decimal pesoKg,
        out bool? estabilidad)
    {
        pesoKg = 0m;
        estabilidad = null;

        var mayus = trama.ToUpperInvariant();

        if (mayus.StartsWith("ST") || mayus.Contains(",ST,") || mayus.Contains(" ST "))
            estabilidad = true;
        else if (mayus.StartsWith("US") || mayus.Contains(",US,") || mayus.Contains("MOTION"))
            estabilidad = false;

        // Sobrecarga / error: no es un peso.
        if (mayus.StartsWith("OL") || mayus.Contains("OVER") || mayus.Contains("ERR"))
            return false;

        var coincidencia = NumeroRegex.Match(trama);
        if (!coincidencia.Success)
            return false;

        var texto = coincidencia.Groups["num"].Value.Replace(',', '.');
        var tienePunto = texto.Contains('.');

        if (!decimal.TryParse(texto, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var valor))
            return false;

        var unidad = coincidencia.Groups["unidad"].Value.ToLowerInvariant();

        // "001234" sin punto ni unidad: el número viene en la mínima
        // división (gramos) y hay que recorrer el punto.
        if (!tienePunto && unidad.Length == 0 && decimalesSinPunto > 0)
            valor /= (decimal)Math.Pow(10, decimalesSinPunto);

        if (coincidencia.Groups["signo"].Value == "-")
            valor = -valor;

        pesoKg = unidad switch
        {
            "lb" => valor * LibraAKg,
            "oz" => valor * LibraAKg / 16m,
            "g" => valor / 1000m,
            _ => valor
        };

        pesoKg = Math.Round(pesoKg, 3, MidpointRounding.AwayFromZero);

        // Una N40 llega a 40 kg: más de 1000 es basura de la trama.
        return pesoKg is > -1000m and < 1000m;
    }


    /// <summary>
    /// Si la báscula no avisa si está estable, se considera estable
    /// cuando el peso no cambia durante ~0.45 s.
    /// </summary>
    private bool CalcularEstabilidad(decimal pesoKg, bool? estabilidadBascula)
    {
        var ahora = DateTime.UtcNow;

        _ultimas.Add((ahora, pesoKg));
        _ultimas.RemoveAll(l => ahora - l.Hora > TimeSpan.FromSeconds(2));

        if (estabilidadBascula.HasValue)
            return estabilidadBascula.Value;

        var ventana = _ultimas
            .Where(l => ahora - l.Hora <= TiempoEstable + TimeSpan.FromMilliseconds(300))
            .ToList();

        if (ventana.Count < 2)
            return false;

        var abarca = ventana[^1].Hora - ventana[0].Hora;
        var rango = ventana.Max(l => l.Peso) - ventana.Min(l => l.Peso);

        if (rango > ToleranciaEstableKg)
            return false;

        // Báscula que manda pocas tramas (modo "a pedido"): basta con
        // que las dos últimas coincidan.
        var entreUltimas = ventana[^1].Hora - ventana[^2].Hora;

        return abarca >= TiempoEstable ||
               entreUltimas >= TimeSpan.FromMilliseconds(300);
    }


    // ============================================================
    // COMANDOS
    // ============================================================

    /// <summary>
    /// Convierte "P", "W\r\n" o "\x05" en los bytes a mandar.
    /// </summary>
    public static byte[] DecodificarComando(string? comando)
    {
        if (string.IsNullOrEmpty(comando))
            return Array.Empty<byte>();

        var texto = Regex.Replace(
            comando,
            @"\\x([0-9A-Fa-f]{2})",
            m => ((char)Convert.ToByte(m.Groups[1].Value, 16)).ToString());

        texto = texto
            .Replace("\\r", "\r")
            .Replace("\\n", "\n");

        return Encoding.ASCII.GetBytes(texto);
    }


    public static string DescribirComando(string? comando) =>
        string.IsNullOrEmpty(comando)
            ? "Envío continuo (la báscula manda el peso sola)"
            : $"Pide el peso con «{comando}»";


    // ============================================================
    // DETECCIÓN AUTOMÁTICA
    // ============================================================

    /// <summary>
    /// Busca la báscula. Por cada puerto:
    ///   1) escucha en cada velocidad (básculas de envío continuo);
    ///   2) si nadie habló, le pide el peso con los comandos comunes.
    /// </summary>
    public static async Task<DeteccionBascula?> DetectarAsync(
        IReadOnlyList<string> puertos,
        int decimalesSinPunto,
        IProgress<string>? progreso,
        Action<string>? tramaCruda,
        CancellationToken token)
    {
        foreach (var puerto in puertos)
        {
            // Pruebas en orden: primero solo escuchar, luego preguntar.
            var pruebas = VelocidadesComunes
                .Select(v => (Velocidad: v, Comando: string.Empty))
                .Concat(
                    VelocidadesComunes.SelectMany(v =>
                        ComandosComunes
                            .Where(c => c.Length > 0)
                            .Select(c => (Velocidad: v, Comando: c))))
                .ToList();

            foreach (var (velocidad, comando) in pruebas)
            {
                token.ThrowIfCancellationRequested();

                progreso?.Report(
                    comando.Length == 0
                        ? $"Escuchando {puerto} a {velocidad} baudios…"
                        : $"Preguntando a {puerto} ({velocidad} baudios) con «{comando}»…");

                var configuracion = new ScaleConfiguration
                {
                    Puerto = puerto,
                    BaudRate = velocidad,
                    Comando = comando,
                    DecimalesSinPunto = decimalesSinPunto
                };

                using var lector = new BasculaLector(configuracion);

                LecturaBascula? encontrada = null;
                var lecturas = 0;

                lector.PesoLeido += l =>
                {
                    if (Interlocked.Increment(ref lecturas) >= 2)
                        encontrada ??= l;
                };

                if (tramaCruda is not null)
                    lector.TramaRecibida += t => tramaCruda($"[{puerto} · {velocidad}] {t}");

                if (!lector.Iniciar(out _))
                    break; // el puerto no abre: pasar al siguiente puerto

                var limite = DateTime.UtcNow.AddMilliseconds(
                    comando.Length == 0 ? 1300 : 1700);

                while (DateTime.UtcNow < limite && encontrada is null)
                    await Task.Delay(100, token);

                lector.Detener();

                if (encontrada is not null)
                    return new DeteccionBascula(puerto, velocidad, comando, encontrada);
            }
        }

        return null;
    }
}
