using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Clientes;

public class ClientePos
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } =
        string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } =
        string.Empty;

    [JsonPropertyName("rfc")]
    public string Rfc { get; set; } =
        string.Empty;

    [JsonPropertyName("dias_de_credito")]
    public int? DiasCredito { get; set; }

    [JsonPropertyName("forma_pago")]
    public string? FormaPago { get; set; }

    [JsonPropertyName("metodo_pago")]
    public string? MetodoPago { get; set; }

    [JsonPropertyName("condiciones_de_pago")]
    public string? CondicionesPago { get; set; }

    [JsonPropertyName("estatus")]
    public int Estatus { get; set; }

    [JsonPropertyName("id_empresa")]
    public int? IdEmpresa { get; set; }


    // =========================================================
    // TEXTO QUE MOSTRAREMOS EN EL AUTOCOMPLETE
    // =========================================================

    [JsonIgnore]
    public string TextoMostrar =>
        Nombre;
}