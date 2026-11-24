using System;
using System.IO;
using System.Text.Json;
using NovaCoreESDM.Models.Productos;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Copia del catálogo (productos, presentaciones y precios) de la
/// unidad operativa. Se reemplaza cada vez que el catálogo se descarga
/// bien, y se usa cuando no hay internet.
/// </summary>
public static class CatalogoLocal
{
    private sealed class Copia
    {
        public DateTime Fecha { get; set; }
        public string Contenido { get; set; } = string.Empty;
    }

    private static string RutaDe(int idUnidadOperativa) =>
        AlmacenLocal.Ruta("catalogo", $"catalogo-{idUnidadOperativa}.json");


    public static void Guardar(int idUnidadOperativa, string contenidoJson)
    {
        try
        {
            AlmacenLocal.Guardar(
                RutaDe(idUnidadOperativa),
                new Copia { Fecha = DateTime.Now, Contenido = contenidoJson });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo guardar la copia del catálogo: {ex.Message}");
        }
    }


    public static CatalogoProductosResponse? Obtener(int idUnidadOperativa)
    {
        var copia = AlmacenLocal.Leer<Copia>(RutaDe(idUnidadOperativa));
        if (copia is null || string.IsNullOrWhiteSpace(copia.Contenido))
            return null;

        try
        {
            var catalogo = JsonSerializer.Deserialize<CatalogoProductosResponse>(
                copia.Contenido,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (catalogo is null)
                return null;

            catalogo.Status = 1;
            catalogo.DesdeCopiaLocal = true;
            catalogo.FechaCopiaLocal = copia.Fecha;
            return catalogo;
        }
        catch
        {
            return null;
        }
    }
}
