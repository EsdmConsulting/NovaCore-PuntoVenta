using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Credito;

public class RegistrarAbonoCreditoRequest
{
    [JsonPropertyName("id_cliente")]
    public int IdCliente { get; set; }

    [JsonPropertyName("modo_aplicacion")]
    public string ModoAplicacion { get; set; } =
        "DOCUMENTOS";

    [JsonPropertyName("aplicaciones")]
    public List<AplicacionDocumentoCredito> Aplicaciones { get; set; } =
        new();


    [JsonPropertyName("id_empresa")]
    public int? IdEmpresa { get; set; }

    [JsonPropertyName("id_unidad_operativa")]
    public int? IdUnidadOperativa { get; set; }

    [JsonPropertyName("id_caja")]
    public int? IdCaja { get; set; }

    [JsonPropertyName("id_turno")]
    public int? IdTurno { get; set; }


    [JsonPropertyName("tipo_pagador")]
    public string TipoPagador { get; set; } =
        "CLIENTE";

    [JsonPropertyName("id_cliente_pagador")]
    public int? IdClientePagador { get; set; }

    [JsonPropertyName("pagador_razon_social")]
    public string? PagadorRazonSocial { get; set; }

    [JsonPropertyName("pagador_rfc")]
    public string? PagadorRfc { get; set; }


    [JsonPropertyName("origen_pago")]
    public string OrigenPago { get; set; } =
        "CAJA";

    [JsonPropertyName("forma_pago")]
    public string FormaPago { get; set; } =
        string.Empty;

    [JsonPropertyName("referencia")]
    public string? Referencia { get; set; }

    [JsonPropertyName("comprobante_path")]
    public string? ComprobantePath { get; set; }

    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }
}


public class AplicacionDocumentoCredito
{
    [JsonPropertyName("id_documento_credito")]
    public int IdDocumentoCredito { get; set; }

    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }
}