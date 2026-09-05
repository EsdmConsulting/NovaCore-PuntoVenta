using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Credito;

public class RegistrarAbonoCreditoResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public RegistrarAbonoCreditoData? Data { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}


public class RegistrarAbonoCreditoData
{
    [JsonPropertyName("id_abono")]
    public int IdAbono { get; set; }

    [JsonPropertyName("modo_aplicacion")]
    public string ModoAplicacion { get; set; } =
        string.Empty;

    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }

    [JsonPropertyName("monto_aplicado")]
    public decimal MontoAplicado { get; set; }

    [JsonPropertyName("monto_sin_aplicar")]
    public decimal MontoSinAplicar { get; set; }

    [JsonPropertyName("estado_abono")]
    public string EstadoAbono { get; set; } =
        string.Empty;

    [JsonPropertyName("documentos_afectados")]
    public int DocumentosAfectados { get; set; }

    [JsonPropertyName("documentos_aplicados")]
    public List<DocumentoCreditoAplicado> DocumentosAplicados { get; set; } =
        new();

    [JsonPropertyName("estado_credito")]
    public EstadoCreditoData? EstadoCredito { get; set; }
}


public class DocumentoCreditoAplicado
{
    [JsonPropertyName("id_documento_credito")]
    public int IdDocumentoCredito { get; set; }

    [JsonPropertyName("tipo_documento")]
    public string TipoDocumento { get; set; } =
        string.Empty;

    [JsonPropertyName("folio")]
    public string Folio { get; set; } =
        string.Empty;

    [JsonPropertyName("monto_aplicado")]
    public decimal MontoAplicado { get; set; }

    [JsonPropertyName("saldo_anterior")]
    public decimal SaldoAnterior { get; set; }

    [JsonPropertyName("saldo_nuevo")]
    public decimal SaldoNuevo { get; set; }

    [JsonPropertyName("estado_anterior")]
    public int EstadoAnterior { get; set; }

    [JsonPropertyName("estado_nuevo")]
    public int EstadoNuevo { get; set; }
}