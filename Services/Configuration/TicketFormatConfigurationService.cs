using System;
using System.IO;
using System.Text.Json;
using NovaCoreESDM.Models.Configuration;

namespace NovaCoreESDM.Services.Configuration;

/// <summary>
/// Carga el formato del ticket desde ticket-format.json.
/// Si el archivo no existe, lo crea con los valores por defecto para
/// que se pueda editar a mano.
/// </summary>
public class TicketFormatConfigurationService
{
    private static readonly string ArchivoFormato =
        Path.Combine(
            PrinterConfigurationService.DirectorioDatos,
            "ticket-format.json");

    private static readonly JsonSerializerOptions Opciones =
        new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };


    public string ObtenerRuta() => ArchivoFormato;


    public TicketFormatConfiguration Obtener()
    {
        try
        {
            if (File.Exists(ArchivoFormato))
            {
                var json = File.ReadAllText(ArchivoFormato);

                var formato =
                    JsonSerializer.Deserialize<TicketFormatConfiguration>(
                        json,
                        Opciones);

                if (formato is not null)
                    return Normalizar(formato);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ticket-format.json inválido, se usa el formato por defecto: {ex.Message}");

            // No se sobrescribe: así el usuario puede corregir su archivo.
            return new TicketFormatConfiguration();
        }

        // No existía: lo creamos con los valores por defecto.
        var porDefecto = new TicketFormatConfiguration();

        try
        {
            Directory.CreateDirectory(
                PrinterConfigurationService.DirectorioDatos);

            File.WriteAllText(
                ArchivoFormato,
                JsonSerializer.Serialize(porDefecto, Opciones));
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"No fue posible crear ticket-format.json: {ex.Message}");
        }

        return porDefecto;
    }


    // Protege la impresión de valores absurdos escritos a mano.
    private static TicketFormatConfiguration Normalizar(
        TicketFormatConfiguration formato)
    {
        formato.AnchoCaracteres =
            Math.Clamp(formato.AnchoCaracteres, 24, 64);

        formato.Encabezado ??= new();
        formato.Pie ??= new();
        formato.MensajeFinal ??= string.Empty;
        formato.TextoQr ??= string.Empty;

        return formato;
    }
}
