namespace NovaCoreESDM.Models.Configuration;

public class TerminalConfiguration
{
    public int IdCaja { get; set; }

    public string UuidCaja { get; set; } = string.Empty;

    public int IdEmpresa { get; set; }

    public int IdUnidadOperativa { get; set; }

    public string UnidadCodigo { get; set; } = string.Empty;

    public string UnidadNombre { get; set; } = string.Empty;

    public string CodigoCaja { get; set; } = string.Empty;

    public string NombreCaja { get; set; } = string.Empty;

    public string? NumeroTerminal { get; set; }

    public string SerieTicket { get; set; } = string.Empty;
    
    public int? IdTurnoActivo { get; set; }
}