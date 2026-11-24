using System.IO;
using System.Text.Json;
using NovaCoreESDM.Models.Configuration;

namespace NovaCoreESDM.Services.Configuration;

/// <summary>
/// Lee, guarda y quita la báscula de este equipo.
/// </summary>
public class ScaleConfigurationService
{
    private static readonly string ArchivoConfiguracion =
        Path.Combine(
            PrinterConfigurationService.DirectorioDatos,
            "scale-config.json");

    private static readonly JsonSerializerOptions Opciones =
        new() { WriteIndented = true };


    public string ObtenerRuta() => ArchivoConfiguracion;


    /// <summary>
    /// Devuelve la báscula configurada, o null si no hay ninguna.
    /// </summary>
    public ScaleConfiguration? Obtener()
    {
        try
        {
            if (!File.Exists(ArchivoConfiguracion))
                return null;

            return JsonSerializer.Deserialize<ScaleConfiguration>(
                File.ReadAllText(ArchivoConfiguracion));
        }
        catch
        {
            // Archivo dañado = como si no hubiera báscula.
            return null;
        }
    }


    /// <summary>
    /// La báscula solo si está activada y tiene puerto.
    /// </summary>
    public ScaleConfiguration? ObtenerActiva() =>
        Obtener() is { EstaLista: true } configuracion
            ? configuracion
            : null;


    public void Guardar(ScaleConfiguration configuracion)
    {
        Directory.CreateDirectory(PrinterConfigurationService.DirectorioDatos);

        File.WriteAllText(
            ArchivoConfiguracion,
            JsonSerializer.Serialize(configuracion, Opciones));
    }


    public void Quitar()
    {
        if (File.Exists(ArchivoConfiguracion))
            File.Delete(ArchivoConfiguracion);
    }
}
