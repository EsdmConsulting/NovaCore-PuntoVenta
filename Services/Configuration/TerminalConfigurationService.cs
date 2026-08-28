using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Configuration;

namespace NovaCoreESDM.Services.Configuration;

public class TerminalConfigurationService
{
    private readonly string _directorioConfiguracion;
    private readonly string _archivoConfiguracion;

    public TerminalConfigurationService()
    {
        var localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            );

        _directorioConfiguracion =
            Path.Combine(
                localAppData,
                "NovaCoreESDM"
            );

        _archivoConfiguracion =
            Path.Combine(
                _directorioConfiguracion,
                "terminal-config.json"
            );
    }

    public bool ExisteConfiguracion()
    {
        return File.Exists(
            _archivoConfiguracion
        );
    }

    public async Task<TerminalConfiguration?>
        ObtenerConfiguracionAsync()
    {
        if (!ExisteConfiguracion())
            return null;

        try
        {
            var json =
                await File.ReadAllTextAsync(
                    _archivoConfiguracion
                );

            return JsonSerializer
                .Deserialize<TerminalConfiguration>(
                    json
                );
        }
        catch
        {
            return null;
        }
    }

    public async Task GuardarConfiguracionAsync(
        TerminalConfiguration configuracion)
    {
        Directory.CreateDirectory(
            _directorioConfiguracion
        );

        var options =
            new JsonSerializerOptions
            {
                WriteIndented = true
            };

        var json =
            JsonSerializer.Serialize(
                configuracion,
                options
            );

        await File.WriteAllTextAsync(
            _archivoConfiguracion,
            json
        );
    }


    // =========================================================
    // GUARDAR TURNO ACTIVO
    // =========================================================

    public async Task GuardarTurnoActivoAsync(
        int idTurno)
    {
        var configuracion =
            await ObtenerConfiguracionAsync();

        if (configuracion is null)
            return;

        configuracion.IdTurnoActivo =
            idTurno;

        await GuardarConfiguracionAsync(
            configuracion
        );
    }


    // =========================================================
    // LIMPIAR TURNO ACTIVO
    // =========================================================

    public async Task LimpiarTurnoActivoAsync()
    {
        var configuracion =
            await ObtenerConfiguracionAsync();

        if (configuracion is null)
            return;

        configuracion.IdTurnoActivo =
            null;

        await GuardarConfiguracionAsync(
            configuracion
        );
    }


    public void EliminarConfiguracion()
    {
        if (File.Exists(
            _archivoConfiguracion))
        {
            File.Delete(
                _archivoConfiguracion
            );
        }
    }

    public string ObtenerRutaConfiguracion()
    {
        return _archivoConfiguracion;
    }
}