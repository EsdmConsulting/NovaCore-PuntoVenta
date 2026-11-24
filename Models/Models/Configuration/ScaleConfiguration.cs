using System;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Configuration;

/// <summary>
/// Báscula electrónica conectada a ESTE equipo (punto de venta).
///
/// Se guarda en:  %LocalAppData%\NovaCoreESDM\scale-config.json
///
/// Igual que la impresora, pertenece al equipo físico y no a la caja,
/// por eso no vive en terminal-config.json.
/// </summary>
public class ScaleConfiguration
{
    /// <summary>
    /// true = al vender por KG el peso se toma de la báscula.
    /// </summary>
    public bool Habilitada { get; set; }

    /// <summary>
    /// Puerto serial de la báscula (Windows: COM3; macOS: /dev/cu.usbserial-…).
    /// </summary>
    public string Puerto { get; set; } = string.Empty;

    /// <summary>
    /// Velocidad del puerto. La mayoría de las básculas usan 9600.
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// Texto que se le envía a la báscula para pedirle el peso.
    /// Vacío = la báscula manda el peso sola (envío continuo).
    /// Se guarda con escapes: "P", "W\r\n", "\x05"…
    /// </summary>
    public string Comando { get; set; } = string.Empty;

    /// <summary>
    /// Si la báscula manda el peso sin punto decimal ("001234"),
    /// cuántos decimales tiene. 3 = gramos → 1.234 kg.
    /// </summary>
    public int DecimalesSinPunto { get; set; } = 3;

    public DateTime FechaConfiguracion { get; set; } = DateTime.Now;

    public string ConfiguradaPor { get; set; } = string.Empty;

    [JsonIgnore]
    public bool EstaLista =>
        Habilitada && !string.IsNullOrWhiteSpace(Puerto);
}
