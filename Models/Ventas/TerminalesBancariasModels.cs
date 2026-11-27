using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

// GET api/pos/cajas/{id}/terminales-bancarias
// Terminales bancarias asignadas a la caja (o a su unidad operativa).

public class TerminalesBancariasResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public TerminalesBancariasData? Data { get; set; }
}

public class TerminalesBancariasData
{
    [JsonPropertyName("terminales")]
    public List<TerminalBancariaCaja> Terminales { get; set; } = new();
}

public class TerminalBancariaCaja
{
    [JsonPropertyName("id_tpv_bancaria")]
    public int IdTpvBancaria { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("banco")]
    public string Banco { get; set; } = string.Empty;

    [JsonPropertyName("afiliacion")]
    public string? Afiliacion { get; set; }

    [JsonPropertyName("modelo")]
    public string? Modelo { get; set; }

    [JsonPropertyName("numero_serie")]
    public string? NumeroSerie { get; set; }

    [JsonPropertyName("es_predeterminada")]
    public bool EsPredeterminada { get; set; }


    /// <summary>Últimos 4 de la afiliación (para el ticket y la lista).</summary>
    [JsonIgnore]
    public string AfiliacionCorta =>
        string.IsNullOrWhiteSpace(Afiliacion)
            ? string.Empty
            : Afiliacion.Trim().Length <= 4
                ? Afiliacion.Trim()
                : Afiliacion.Trim()[^4..];

    /// <summary>"BBVA · Afil. 4738793 · PAX S920"</summary>
    [JsonIgnore]
    public string Texto
    {
        get
        {
            var partes = new List<string>();

            var titulo = string.IsNullOrWhiteSpace(Banco) ? Nombre : Banco.Trim().ToUpperInvariant();
            partes.Add(titulo);

            if (!string.IsNullOrWhiteSpace(Afiliacion))
                partes.Add($"Afil. {Afiliacion.Trim()}");

            if (!string.IsNullOrWhiteSpace(Modelo))
                partes.Add(Modelo.Trim());

            if (!string.IsNullOrWhiteSpace(NumeroSerie))
                partes.Add($"serie {NumeroSerie.Trim()}");

            return string.Join(" · ", partes);
        }
    }

    /// <summary>"BBVA AFIL ***8793" (cabe en el ticket y en la referencia del pago).</summary>
    [JsonIgnore]
    public string TextoCorto
    {
        get
        {
            var banco = string.IsNullOrWhiteSpace(Banco) ? Nombre.Trim() : Banco.Trim();
            banco = banco.ToUpperInvariant();

            return string.IsNullOrWhiteSpace(AfiliacionCorta)
                ? banco
                : $"{banco} AFIL ***{AfiliacionCorta}";
        }
    }

    public override string ToString() => Texto;
}
