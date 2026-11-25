using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NovaCoreESDM.Models.Configuration;

namespace NovaCoreESDM.Services.Configuration;

/// <summary>
/// Terminales de cobro con tarjeta de este equipo.
/// Archivo: %LocalAppData%\NovaCoreESDM\payment-terminals.json
/// (aparte de terminal-config.json: "Cambiar caja" no debe borrarlas).
/// </summary>
public class TerminalPagoConfigurationService
{
    private static readonly string Archivo =
        Path.Combine(
            PrinterConfigurationService.DirectorioDatos,
            "payment-terminals.json");

    private static readonly JsonSerializerOptions Opciones =
        new() { WriteIndented = true };

    public string ObtenerRuta() => Archivo;


    public List<TerminalPagoConfiguration> ObtenerTodas()
    {
        try
        {
            if (!File.Exists(Archivo))
                return new();

            var json = File.ReadAllText(Archivo);

            return JsonSerializer
                       .Deserialize<TerminalesPagoConfiguration>(json)
                       ?.Terminales
                   ?? new();
        }
        catch
        {
            return new();
        }
    }

    public List<TerminalPagoConfiguration> ObtenerActivas() =>
        ObtenerTodas().Where(t => t.Activa).ToList();

    public bool HayTerminalesActivas() =>
        ObtenerTodas().Any(t => t.Activa);


    /// <summary>Alta o actualización (por Id).</summary>
    public void Guardar(TerminalPagoConfiguration terminal)
    {
        var lista = ObtenerTodas();

        var indice = lista.FindIndex(t => t.Id == terminal.Id);

        if (indice >= 0)
            lista[indice] = terminal;
        else
            lista.Add(terminal);

        Escribir(lista);
    }

    public void Quitar(string id)
    {
        var lista = ObtenerTodas();

        lista.RemoveAll(t => t.Id == id);

        Escribir(lista);
    }

    private static void Escribir(List<TerminalPagoConfiguration> lista)
    {
        Directory.CreateDirectory(PrinterConfigurationService.DirectorioDatos);

        File.WriteAllText(
            Archivo,
            JsonSerializer.Serialize(
                new TerminalesPagoConfiguration { Terminales = lista },
                Opciones));
    }
}
