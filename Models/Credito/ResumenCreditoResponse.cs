using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Credito;

public class ResumenCreditoResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public ResumenCreditoData? Data { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}


public class ResumenCreditoData
{
    [JsonPropertyName("cliente")]
    public ClienteCreditoResumen? Cliente { get; set; }

    [JsonPropertyName("credito")]
    public EstadoCreditoData? Credito { get; set; }

    [JsonPropertyName("analisis_disponibles")]
    public List<object> AnalisisDisponibles { get; set; } =
        new();

    [JsonPropertyName("documentos_pendientes")]
    public List<DocumentoCreditoPendiente> DocumentosPendientes { get; set; } =
        new();
}


public class ClienteCreditoResumen
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

    [JsonPropertyName("estatus")]
    public int Estatus { get; set; }
}


public class DocumentoCreditoPendiente
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("tipo_documento")]
    public string TipoDocumento { get; set; } =
        string.Empty;

    [JsonPropertyName("folio")]
    public string Folio { get; set; } =
        string.Empty;

    [JsonPropertyName("uuid_cfdi")]
    public string? UuidCfdi { get; set; }

    [JsonPropertyName("metodo_pago")]
    public string MetodoPago { get; set; } =
        string.Empty;

    [JsonPropertyName("fecha_emision")]
    public string? FechaEmision { get; set; }

    [JsonPropertyName("fecha_vencimiento")]
    public string? FechaVencimiento { get; set; }

    [JsonPropertyName("importe_total")]
    public decimal ImporteTotal { get; set; }

    [JsonPropertyName("importe_pagado")]
    public decimal ImportePagado { get; set; }

    [JsonPropertyName("saldo")]
    public decimal Saldo { get; set; }

    [JsonPropertyName("estado")]
    public int Estado { get; set; }

    [JsonPropertyName("requiere_rep")]
    public bool RequiereRep { get; set; }
}