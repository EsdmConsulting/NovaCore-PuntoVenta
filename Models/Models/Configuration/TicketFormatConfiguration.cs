using System.Collections.Generic;

namespace NovaCoreESDM.Models.Configuration;

/// <summary>
/// Opciones de diseño del ticket.
///
/// Se guardan en:  %LocalAppData%\NovaCoreESDM\ticket-format.json
///
/// La primera vez que se imprime, el archivo se crea solo con estos
/// valores. Para "diseñar" el ticket basta con editar ese archivo con
/// el Bloc de notas y volver a imprimir (no hay que recompilar).
/// </summary>
public class TicketFormatConfiguration
{
    // Caracteres por línea.
    //   80 mm  -> 42 (o 48 según la impresora)
    //   58 mm  -> 32
    public int AnchoCaracteres { get; set; } = 42;

    public bool MostrarLogo { get; set; } = true;

    // Líneas en negritas, centradas, debajo del logo.
    public List<string> Encabezado { get; set; } =
        new() { "GRUPO LUISFER", "ESDM" };

    public bool MostrarUnidadOperativa { get; set; } = true;

    public bool MostrarCajero { get; set; } = true;

    public bool MostrarImpuestos { get; set; } = true;

    // Mensaje en negritas al final.
    public string MensajeFinal { get; set; } =
        "GRACIAS POR SU COMPRA";

    // Líneas extra centradas debajo del mensaje (promociones, leyendas, etc.)
    public List<string> Pie { get; set; } = new();

    public bool MostrarQr { get; set; } = true;

    public string TextoQr { get; set; } =
        "Escanea para ingresar a Plataforma LuisFer y Facturar";

    public int LineasEnBlancoAlFinal { get; set; } = 3;

    public bool CortarPapel { get; set; } = true;
}
