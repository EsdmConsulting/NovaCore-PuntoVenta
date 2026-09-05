using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Credito;


// =========================================================
// REQUEST
// =========================================================

public class EstadoCreditoRequest
{
    [JsonPropertyName("id_cliente")]
    public int IdCliente { get; set; }


    [JsonPropertyName("monto_nueva_venta")]
    public decimal MontoNuevaVenta { get; set; }
}


// =========================================================
// RESPONSE
// =========================================================

public class EstadoCreditoResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }


    [JsonPropertyName("msg")]
    public string Msg { get; set; } =
        string.Empty;


    [JsonPropertyName("data")]
    public EstadoCreditoData? Data { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; } =
        string.Empty;


    [JsonPropertyName("title")]
    public string Title { get; set; } =
        string.Empty;
}


// =========================================================
// ESTADO DE CRÉDITO
// =========================================================

public class EstadoCreditoData
{
    [JsonPropertyName("id_cliente")]
    public int IdCliente { get; set; }

    [JsonPropertyName("id_credito")]
    public int? IdCredito { get; set; }

    [JsonPropertyName("id_analisis_credito")]
    public int? IdAnalisisCredito { get; set; }

    [JsonPropertyName("limite_credito")]
    public decimal LimiteCredito { get; set; }

    [JsonPropertyName("saldo_deudor")]
    public decimal SaldoDeudor { get; set; }

    [JsonPropertyName("saldo_vencido")]
    public decimal SaldoVencido { get; set; }

    [JsonPropertyName("dias_max_vencido")]
    public int DiasMaxVencido { get; set; }

    [JsonPropertyName("dias_credito")]
    public int DiasCredito { get; set; }

    [JsonPropertyName("tolerancia_dias")]
    public int ToleranciaDias { get; set; }

    [JsonPropertyName("fecha_inicio")]
    public string FechaInicio { get; set; } =
        string.Empty;

    [JsonPropertyName("fecha_vencimiento")]
    public string FechaVencimiento { get; set; } =
        string.Empty;

    [JsonPropertyName("disponible")]
    public decimal Disponible { get; set; }

    [JsonPropertyName("monto_nueva_venta")]
    public decimal MontoNuevaVenta { get; set; }

    [JsonPropertyName("saldo_proyectado")]
    public decimal SaldoProyectado { get; set; }

    [JsonPropertyName("disponible_proyectado")]
    public decimal DisponibleProyectado { get; set; }

    [JsonPropertyName("puede_vender_credito")]
    public bool PuedeVenderCredito { get; set; }

    [JsonPropertyName("semaforo")]
    public string Semaforo { get; set; } =
        string.Empty;

    [JsonPropertyName("motivos_bloqueo")]
    public List<string> MotivosBloqueo { get; set; } =
        new();

    [JsonPropertyName("maneja_credito")]
    public bool ManejaCredito { get; set; }
}