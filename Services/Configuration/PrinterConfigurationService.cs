using System;
using System.IO;
using System.Text.Json;
using NovaCoreESDM.Models.Configuration;

namespace NovaCoreESDM.Services.Configuration;

/// <summary>
/// Lee, guarda y quita la impresora fija de este equipo.
/// </summary>
public class PrinterConfigurationService
{
    // Carpeta donde NovaCore guarda sus archivos locales:
    // C:\Users\<usuario>\AppData\Local\NovaCoreESDM
    public static string DirectorioDatos { get; } =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "NovaCoreESDM");

    private static readonly string ArchivoConfiguracion =
        Path.Combine(DirectorioDatos, "printer-config.json");

    private static readonly JsonSerializerOptions Opciones =
        new() { WriteIndented = true };


    public string ObtenerRuta() => ArchivoConfiguracion;


    /// <summary>
    /// Devuelve la impresora fija, o null si no hay ninguna configurada.
    /// </summary>
    public PrinterConfiguration? Obtener()
    {
        try
        {
            if (!File.Exists(ArchivoConfiguracion))
                return null;

            var json = File.ReadAllText(ArchivoConfiguracion);

            var configuracion =
                JsonSerializer.Deserialize<PrinterConfiguration>(json);

            return configuracion is { TieneImpresora: true }
                ? configuracion
                : null;
        }
        catch
        {
            // Archivo dañado = como si no hubiera impresora configurada.
            return null;
        }
    }


    public string? ObtenerNombreImpresora() =>
        Obtener()?.NombreImpresora;


    public void Guardar(PrinterConfiguration configuracion)
    {
        Directory.CreateDirectory(DirectorioDatos);

        File.WriteAllText(
            ArchivoConfiguracion,
            JsonSerializer.Serialize(configuracion, Opciones));
    }


    /// <summary>
    /// Deja el equipo SIN impresora fija (para poder elegir otra).
    /// </summary>
    public void Quitar()
    {
        if (File.Exists(ArchivoConfiguracion))
            File.Delete(ArchivoConfiguracion);
    }
}
