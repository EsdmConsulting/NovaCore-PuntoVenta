using System;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Configuration;

/// <summary>
/// Impresora de tickets asignada a ESTE equipo (punto de venta).
///
/// Se guarda en:  %LocalAppData%\NovaCoreESDM\printer-config.json
///
/// IMPORTANTE: es un archivo distinto a terminal-config.json porque
/// "Cambiar caja" borra terminal-config.json, y la impresora pertenece
/// al equipo físico, no a la caja.
/// </summary>
public class PrinterConfiguration
{
    public string NombreImpresora { get; set; } = string.Empty;

    public DateTime FechaConfiguracion { get; set; } = DateTime.Now;

    public string ConfiguradaPor { get; set; } = string.Empty;

    [JsonIgnore]
    public bool TieneImpresora =>
        !string.IsNullOrWhiteSpace(NombreImpresora);
}
