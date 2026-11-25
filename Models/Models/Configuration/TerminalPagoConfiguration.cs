using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Configuration;

public enum TipoConexionTerminal
{
    Alambrica = 0,
    Inalambrica = 1
}

/// <summary>
/// Terminal bancaria (cobro con tarjeta) vinculada a ESTE equipo.
///
/// - Alámbrica: solo se registra; el cobro es manual (el cajero captura
///   las terminaciones del número de pago que imprimió la terminal).
/// - Inalámbrica: además lleva los datos de red para que el POS le envíe
///   el importe y reciba el resultado automáticamente.
/// </summary>
public class TerminalPagoConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Nombre { get; set; } = string.Empty;

    public TipoConexionTerminal Tipo { get; set; } = TipoConexionTerminal.Alambrica;

    public bool Activa { get; set; } = true;

    // ----- Red (solo inalámbricas) -----

    /// <summary>IP o nombre de host de la terminal / pasarela en la red.</summary>
    public string Host { get; set; } = string.Empty;

    public int Puerto { get; set; } = 8091;

    public bool UsarHttps { get; set; }

    /// <summary>Código con el que la terminal se identifica en su API.</summary>
    public string CodigoTerminal { get; set; } = string.Empty;

    /// <summary>Clave de acceso (Bearer) de la terminal.</summary>
    public string ClaveApi { get; set; } = string.Empty;

    public int TimeoutSegundos { get; set; } = 5;

    public DateTime FechaConfiguracion { get; set; } = DateTime.Now;

    public string ConfiguradaPor { get; set; } = string.Empty;

    [JsonIgnore]
    public bool EsInalambrica => Tipo == TipoConexionTerminal.Inalambrica;

    [JsonIgnore]
    public string TipoTexto => EsInalambrica ? "Inalámbrica" : "Alámbrica";

    [JsonIgnore]
    public string Direccion =>
        EsInalambrica
            ? $"{(UsarHttps ? "https" : "http")}://{Host}:{Puerto}"
            : "Cobro manual";

    [JsonIgnore]
    public string Resumen =>
        EsInalambrica
            ? $"{TipoTexto} · {Direccion} · {CodigoTerminal}"
            : $"{TipoTexto} · captura manual de terminaciones";

    public override string ToString() => Nombre;
}

public class TerminalesPagoConfiguration
{
    public List<TerminalPagoConfiguration> Terminales { get; set; } = new();
}
