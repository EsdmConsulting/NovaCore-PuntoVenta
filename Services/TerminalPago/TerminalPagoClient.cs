using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Configuration;
using NovaCoreESDM.Models.TerminalPago;

namespace NovaCoreESDM.Services.TerminalPago;

/// <summary>
/// Comunicación DIRECTA POS -> terminal inalámbrica por la red local.
///
/// Usa su propio HttpClient (no ApiClient): la terminal no es el ERP,
/// no comparte cookies y cada terminal tiene su propia dirección y clave.
///
/// Esta clase NUNCA registra pagos: solo cobra en la terminal. El pago
/// se registra en el ERP después, con VentasViewModel.RegistrarPagoTarjetaAsync.
/// </summary>
public class TerminalPagoClient
{
    private const string Prefijo = "api/v1";

    // Referencias de cobros directos ya registradas en el ERP durante esta
    // sesión del POS (evita reanudar un cobro aprobado que ya se aplicó).
    private static readonly HashSet<string> ReferenciasRegistradas = new();

    public static void MarcarRegistrada(string referencia)
    {
        if (!string.IsNullOrWhiteSpace(referencia))
            ReferenciasRegistradas.Add(referencia);
    }

    private static readonly HttpClient Http = new()
    {
        // El tiempo límite real se aplica por petición (CancellationToken).
        Timeout = Timeout.InfiniteTimeSpan
    };


    // =========================================================
    // PROBAR CONEXIÓN
    // =========================================================

    public async Task<ResultadoTerminal<string>> ProbarConexionAsync(
        TerminalPagoConfiguration terminal)
    {
        var invalida = Validar(terminal);

        if (invalida is not null)
            return ResultadoTerminal<string>.Falla(invalida);

        // 1. ¿Hay algo escuchando? (health es público)
        var salud = await EnviarAsync(terminal, HttpMethod.Get, "health", null, null, autenticar: false);

        if (!salud.Ok)
            return ResultadoTerminal<string>.Falla(salud.Mensaje, salud.SinComunicacion);

        // 2. ¿La clave y el código son correctos?
        var lista = await EnviarAsync(terminal, HttpMethod.Get, "terminals", null, null);

        if (!lista.Ok)
            return ResultadoTerminal<string>.Falla(lista.Mensaje, lista.SinComunicacion);

        var encontrada =
            (lista.Dato?["data"] as JsonArray)?
            .OfType<JsonObject>()
            .FirstOrDefault(t =>
                string.Equals(
                    (string?)t["code"],
                    terminal.CodigoTerminal.Trim(),
                    StringComparison.OrdinalIgnoreCase));

        if (encontrada is null)
        {
            return ResultadoTerminal<string>.Falla(
                $"La red responde, pero no existe la terminal «{terminal.CodigoTerminal}». Revisa el código.");
        }

        if (!((bool?)encontrada["active"] ?? false))
            return ResultadoTerminal<string>.Falla("La terminal está desactivada.");

        if (!((bool?)encontrada["connected"] ?? false))
            return ResultadoTerminal<string>.Falla("La terminal existe pero está desconectada.");

        return ResultadoTerminal<string>.Exito(
            $"Conexión correcta con «{(string?)encontrada["name"] ?? terminal.CodigoTerminal}».");
    }


    // =========================================================
    // ENVIAR COBRO
    // =========================================================

    /// <summary>
    /// Envía el importe a la pantalla de la terminal. Si la venta ya tiene
    /// un cobro abierto por el MISMO importe lo reanuda (no duplica cobros).
    /// <paramref name="claveVenta"/> identifica la venta de forma única
    /// (ver <see cref="ClaveVenta"/>): las ventas sin conexión no tienen id.
    /// </summary>
    public async Task<ResultadoTerminal<CobroTerminal>> EnviarCobroAsync(
        TerminalPagoConfiguration terminal,
        string claveVenta,
        string folio,
        decimal importe,
        string? operador = null)
    {
        var invalida = Validar(terminal);

        if (invalida is not null)
            return ResultadoTerminal<CobroTerminal>.Falla(invalida);

        var centavos = (long)Math.Round(importe * 100m, MidpointRounding.AwayFromZero);

        if (centavos <= 0)
            return ResultadoTerminal<CobroTerminal>.Falla("El importe a cobrar no es válido.");

        if (string.IsNullOrWhiteSpace(claveVenta))
            return ResultadoTerminal<CobroTerminal>.Falla("No hay una venta activa para cobrar.");

        var prefijo = $"{claveVenta}-";

        // 1. Cobros previos de esta venta.
        var previos = await EnviarAsync(
            terminal, HttpMethod.Get,
            $"payments?limit=100&q={Uri.EscapeDataString(claveVenta)}",
            null, null);

        if (!previos.Ok)
            return ResultadoTerminal<CobroTerminal>.Falla(previos.Mensaje, previos.SinComunicacion);

        var ultimoIntento = 0;

        var lista =
            (previos.Dato?["data"] as JsonArray)?
            .Deserialize<List<CobroTerminal>>()
            ?? new();

        foreach (var p in lista)
        {
            if (!p.ReferenciaExterna.StartsWith(prefijo, StringComparison.Ordinal))
                continue;

            if (int.TryParse(p.ReferenciaExterna[prefijo.Length..], out var n))
                ultimoIntento = Math.Max(ultimoIntento, n);

            // Aprobado en la terminal pero todavía NO registrado en el ERP
            // (ej. falló la red al registrar): se reanuda para registrarlo,
            // jamás se cobra otra vez al cliente.
            if (p.Aprobado &&
                p.ImporteCentavos == centavos &&
                !ReferenciasRegistradas.Contains(p.Referencia))
            {
                return ResultadoTerminal<CobroTerminal>.Exito(p);
            }

            if (!p.Abierto)
                continue;

            if (p.ImporteCentavos != centavos)
            {
                return ResultadoTerminal<CobroTerminal>.Falla(
                    "Esta venta ya tiene un cobro abierto en la terminal por otro importe. " +
                    "Cancélalo en la terminal o espera a que expire.");
            }

            return ResultadoTerminal<CobroTerminal>.Exito(p);
        }

        // 2. Cobro nuevo. La clave de idempotencia evita duplicarlo si
        //    la respuesta se pierde y el cajero vuelve a intentar.
        var externa = $"{prefijo}{ultimoIntento + 1}";

        var claveIdempotencia = $"{externa}-{terminal.Id[..Math.Min(8, terminal.Id.Length)]}";

        var cuerpo = new JsonObject
        {
            ["terminal_code"] = terminal.CodigoTerminal.Trim().ToUpperInvariant(),
            ["amount_cents"] = centavos,
            ["currency"] = "MXN",
            ["external_reference"] = externa,
            ["description"] = $"Venta POS {Recortar(folio, 60)}".Trim(),
            // El puente BBVA lo manda al banco como operador (sale en el pagaré).
            ["operator"] = Recortar(operador, 30)
        };

        var creado = await EnviarAsync(
            terminal, HttpMethod.Post, "payments", cuerpo, claveIdempotencia);

        if (!creado.Ok)
            return ResultadoTerminal<CobroTerminal>.Falla(creado.Mensaje, creado.SinComunicacion);

        return LeerCobro(creado.Dato);
    }


    /// <summary>
    /// Clave de la venta en la terminal: POS-{id} para ventas del servidor y
    /// POS-L{uuid} para ventas sin conexión (todas tienen id 0).
    /// </summary>
    public static string ClaveVenta(int idVenta, string? uuidVenta)
    {
        if (idVenta > 0)
            return $"POS-{idVenta}";

        var uuid = (uuidVenta ?? string.Empty).Replace("-", string.Empty);

        return uuid.Length == 0
            ? string.Empty
            : $"POS-L{uuid[..Math.Min(16, uuid.Length)].ToUpperInvariant()}";
    }


    // =========================================================
    // CONSULTAR / CANCELAR
    // =========================================================

    public async Task<ResultadoTerminal<CobroTerminal>> ConsultarAsync(
        TerminalPagoConfiguration terminal,
        string referencia)
    {
        var r = await EnviarAsync(
            terminal, HttpMethod.Get,
            $"payments/{Uri.EscapeDataString(referencia)}", null, null);

        return r.Ok
            ? LeerCobro(r.Dato)
            : ResultadoTerminal<CobroTerminal>.Falla(r.Mensaje, r.SinComunicacion);
    }

    public async Task<ResultadoTerminal<CobroTerminal>> CancelarAsync(
        TerminalPagoConfiguration terminal,
        string referencia)
    {
        var r = await EnviarAsync(
            terminal, HttpMethod.Post,
            $"payments/{Uri.EscapeDataString(referencia)}/cancel",
            new JsonObject(), null);

        return r.Ok
            ? LeerCobro(r.Dato)
            : ResultadoTerminal<CobroTerminal>.Falla(r.Mensaje, r.SinComunicacion);
    }


    /// <summary>
    /// Regresa el cargo de un cobro APROBADO (solo el puente BBVA): el puente
    /// decide CANCELACIÓN (mismo día bancario, por el total) o DEVOLUCIÓN
    /// (pide la tarjeta del cliente). Es asíncrono: se consulta con
    /// <see cref="ConsultarAsync"/> hasta que <c>Reverso</c> termine.
    /// </summary>
    public async Task<ResultadoTerminal<CobroTerminal>> ReversarAsync(
        TerminalPagoConfiguration terminal,
        string referencia,
        decimal importe,
        string? operador)
    {
        var cuerpo = new JsonObject
        {
            ["amount_cents"] = (long)Math.Round(importe * 100m, MidpointRounding.AwayFromZero),
            ["operator"] = Recortar(operador, 30)
        };

        var r = await EnviarAsync(
            terminal, HttpMethod.Post,
            $"payments/{Uri.EscapeDataString(referencia)}/void",
            cuerpo, null);

        return r.Ok
            ? LeerCobro(r.Dato)
            : ResultadoTerminal<CobroTerminal>.Falla(r.Mensaje, r.SinComunicacion);
    }


    // =========================================================
    // INTERNOS
    // =========================================================

    private static string? Validar(TerminalPagoConfiguration t)
    {
        if (!t.EsInalambrica)
            return "Esta terminal es alámbrica: solo admite captura manual.";

        if (string.IsNullOrWhiteSpace(t.Host))
            return "Falta la dirección IP o el nombre de host de la terminal.";

        if (t.Puerto is < 1 or > 65535)
            return "El puerto debe estar entre 1 y 65535.";

        if (string.IsNullOrWhiteSpace(t.CodigoTerminal))
            return "Falta el código de la terminal.";

        if (string.IsNullOrWhiteSpace(t.ClaveApi))
            return "Falta la clave de acceso de la terminal.";

        return null;
    }

    private static string Recortar(string? texto, int max)
    {
        texto ??= string.Empty;
        return texto.Length <= max ? texto : texto[..max];
    }

    private static ResultadoTerminal<CobroTerminal> LeerCobro(JsonNode? raiz)
    {
        try
        {
            var cobro = raiz?["data"]?.Deserialize<CobroTerminal>();

            return cobro is null || string.IsNullOrWhiteSpace(cobro.Referencia)
                ? ResultadoTerminal<CobroTerminal>.Falla("La terminal respondió con un formato desconocido.")
                : ResultadoTerminal<CobroTerminal>.Exito(cobro);
        }
        catch (Exception)
        {
            return ResultadoTerminal<CobroTerminal>.Falla("La terminal respondió con un formato desconocido.");
        }
    }

    private static async Task<ResultadoTerminal<JsonNode>> EnviarAsync(
        TerminalPagoConfiguration terminal,
        HttpMethod metodo,
        string ruta,
        JsonObject? cuerpo,
        string? idempotencia,
        bool autenticar = true)
    {
        var url = $"{(terminal.UsarHttps ? "https" : "http")}://{terminal.Host.Trim()}:{terminal.Puerto}/{Prefijo}/{ruta}";

        using var peticion = new HttpRequestMessage(metodo, url);

        peticion.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (autenticar)
        {
            peticion.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", terminal.ClaveApi.Trim());
        }

        if (!string.IsNullOrEmpty(idempotencia))
            peticion.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencia);

        if (cuerpo is not null)
        {
            peticion.Content = new StringContent(
                cuerpo.ToJsonString(), Encoding.UTF8, "application/json");
        }

        using var limite = new CancellationTokenSource(
            TimeSpan.FromSeconds(Math.Clamp(terminal.TimeoutSegundos, 1, 60)));

        try
        {
            using var respuesta = await Http.SendAsync(peticion, limite.Token);

            var texto = await respuesta.Content.ReadAsStringAsync(limite.Token);

            JsonNode? json = null;

            try { json = JsonNode.Parse(texto); } catch (JsonException) { }

            if (respuesta.IsSuccessStatusCode && json is not null)
                return ResultadoTerminal<JsonNode>.Exito(json);

            if (respuesta.IsSuccessStatusCode)
                return ResultadoTerminal<JsonNode>.Falla("La terminal respondió con un formato desconocido.");

            return ResultadoTerminal<JsonNode>.Falla(
                TraducirError(
                    (int)respuesta.StatusCode,
                    (string?)json?["error"]?["code"],
                    (string?)json?["error"]?["message"]));
        }
        catch (OperationCanceledException)
        {
            return ResultadoTerminal<JsonNode>.Falla(
                $"La terminal no respondió en {terminal.TimeoutSegundos} s. Revisa la red.", sinComunicacion: true);
        }
        catch (HttpRequestException ex)
        {
            return ResultadoTerminal<JsonNode>.Falla(
                $"No se pudo contactar con la terminal ({terminal.Host}:{terminal.Puerto}): {ex.Message}",
                sinComunicacion: true);
        }
    }

    private static string TraducirError(int http, string? codigo, string? mensaje) =>
        codigo switch
        {
            "UNAUTHENTICATED" => "La terminal rechazó la clave de acceso. Revísala en la configuración.",
            "TERMINAL_BUSY" => "La terminal ya tiene un cobro pendiente. Termínalo o cancélalo primero.",
            "TERMINAL_OFFLINE" => "La terminal está desconectada.",
            "TERMINAL_INACTIVE" => "La terminal está desactivada.",
            "TERMINAL_NOT_FOUND" => "La terminal indicada no existe. Revisa el código.",
            "NOT_FOUND" => "La operación no existe en la terminal.",
            "INVALID_TRANSITION" => "La operación ya no puede cancelarse.",
            "INDETERMINATE_STATE" => "El resultado es incierto. Consulta de nuevo en unos segundos.",
            "IN_FLIGHT" => "La operación ya se está procesando en la terminal.",
            "ALREADY_VOIDED" => "Ese cobro ya se canceló o devolvió en el banco.",
            "INVALID_AMOUNT" => "El importe no es válido para el lector.",
            _ => string.IsNullOrWhiteSpace(mensaje)
                ? $"La terminal respondió con error HTTP {http}."
                : mensaje
        };
}
