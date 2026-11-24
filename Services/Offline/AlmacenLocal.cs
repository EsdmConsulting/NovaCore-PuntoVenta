using System;
using System.IO;
using System.Text.Json;
using NovaCoreESDM.Services.Configuration;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Guarda archivos JSON en el equipo de forma segura ante apagones:
/// escribe primero en un temporal, lo fuerza a disco y luego lo
/// reemplaza de golpe. Si se va la luz a la mitad, queda el archivo
/// anterior completo o el nuevo completo, nunca uno a medias.
/// </summary>
public static class AlmacenLocal
{
    public static string Directorio { get; } =
        Path.Combine(PrinterConfigurationService.DirectorioDatos, "offline");

    public static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };


    public static string Ruta(params string[] partes)
    {
        var ruta = Path.Combine(Directorio, Path.Combine(partes));
        var carpeta = Path.GetDirectoryName(ruta);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);
        return ruta;
    }


    public static void Guardar<T>(string ruta, T valor)
    {
        var carpeta = Path.GetDirectoryName(ruta)!;
        Directory.CreateDirectory(carpeta);

        var temporal = ruta + ".tmp";
        var bytes = JsonSerializer.SerializeToUtf8Bytes(valor, Opciones);

        using (var archivo = new FileStream(
                   temporal, FileMode.Create, FileAccess.Write,
                   FileShare.None, 4096, FileOptions.WriteThrough))
        {
            archivo.Write(bytes, 0, bytes.Length);
            archivo.Flush(flushToDisk: true);
        }

        if (File.Exists(ruta))
            File.Replace(temporal, ruta, null, ignoreMetadataErrors: true);
        else
            File.Move(temporal, ruta);
    }


    public static T? Leer<T>(string ruta) where T : class
    {
        try
        {
            if (!File.Exists(ruta))
            {
                // Si el apagón ocurrió justo antes de reemplazar,
                // el temporal completo es la versión más nueva.
                var temporal = ruta + ".tmp";
                if (!File.Exists(temporal))
                    return null;
                return JsonSerializer.Deserialize<T>(
                    File.ReadAllBytes(temporal), Opciones);
            }

            return JsonSerializer.Deserialize<T>(
                File.ReadAllBytes(ruta), Opciones);
        }
        catch
        {
            return null;
        }
    }


    public static void Borrar(string ruta)
    {
        try
        {
            if (File.Exists(ruta)) File.Delete(ruta);
            if (File.Exists(ruta + ".tmp")) File.Delete(ruta + ".tmp");
        }
        catch
        {
            // Si no se puede borrar, se reintentará después.
        }
    }
}
