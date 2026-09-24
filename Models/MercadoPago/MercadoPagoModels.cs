using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Desktop.Models.MercadoPago
{
    // ============================================================
    // TERMINALES
    // GET /api/pos/mercadopago/terminales
    // ============================================================

    public class MercadoPagoTerminalesResponse
    {
        [JsonPropertyName("res")]
        public int Res { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("data")]
        public MercadoPagoTerminalesData? Data { get; set; }
    }


    public class MercadoPagoTerminalesData
    {
        [JsonPropertyName("id_venta")]
        public long? IdVenta { get; set; }

        [JsonPropertyName("folio")]
        public string? Folio { get; set; }

        [JsonPropertyName("id_caja")]
        public int? IdCaja { get; set; }

        [JsonPropertyName("id_tpv_predeterminada")]
        public int? IdTpvPredeterminada { get; set; }

        [JsonPropertyName("total_terminales")]
        public int TotalTerminales { get; set; }

        [JsonPropertyName("terminales")]
        public List<MercadoPagoTerminal> Terminales { get; set; } = new();
    }


    public class MercadoPagoTerminal
    {
        [JsonPropertyName("id_tpv_bancaria")]
        public int IdTpvBancaria { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("banco")]
        public string Banco { get; set; } = string.Empty;

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [JsonPropertyName("numero_serie")]
        public string NumeroSerie { get; set; } = string.Empty;

        [JsonPropertyName("terminal_id")]
        public string TerminalId { get; set; } = string.Empty;

        [JsonPropertyName("pos_id")]
        public string? PosId { get; set; }

        [JsonPropertyName("store_id")]
        public string? StoreId { get; set; }

        [JsonPropertyName("id_cuenta_mercadopago")]
        public int IdCuentaMercadoPago { get; set; }

        [JsonPropertyName("nombre_cuenta_mercadopago")]
        public string NombreCuentaMercadoPago { get; set; } = string.Empty;

        [JsonPropertyName("ambiente")]
        public string Ambiente { get; set; } = string.Empty;

        [JsonPropertyName("asociada_caja")]
        public bool AsociadaCaja { get; set; }

        [JsonPropertyName("es_predeterminada")]
        public bool EsPredeterminada { get; set; }


        // --------------------------------------------------------
        // Propiedades únicamente para presentación en Avalonia.
        // NO vienen del backend.
        // --------------------------------------------------------

        [JsonIgnore]
        public string NombreVisual
        {
            get
            {
                if (EsPredeterminada)
                    return $"{Nombre} (Predeterminada)";

                return Nombre;
            }
        }

        [JsonIgnore]
        public string DescripcionVisual
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(NumeroSerie))
                    return $"S/N: {NumeroSerie}";

                return $"TPV #{IdTpvBancaria}";
            }
        }
    }


    // ============================================================
    // CREAR ORDER
    // POST /api/pos/mercadopago/crear-orden
    // ============================================================

    public class MercadoPagoCrearOrdenRequest
    {
        [JsonPropertyName("id_venta")]
        public long IdVenta { get; set; }

        [JsonPropertyName("id_tpv_bancaria")]
        public int IdTpvBancaria { get; set; }
    }


    public class MercadoPagoCrearOrdenResponse
    {
        [JsonPropertyName("res")]
        public int Res { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("recuperada")]
        public bool Recuperada { get; set; }

        [JsonPropertyName("estado_incierto")]
        public bool EstadoIncierto { get; set; }

        [JsonPropertyName("terminal_ocupada")]
        public bool TerminalOcupada { get; set; }

        [JsonPropertyName("id_operacion")]
        public long? IdOperacionError { get; set; }

        [JsonPropertyName("id_tpv_bancaria")]
        public int? IdTpvBancariaError { get; set; }

        [JsonPropertyName("pago_completo")]
        public bool PagoCompleto { get; set; }

        [JsonPropertyName("estado_venta")]
        public string? EstadoVenta { get; set; }

        [JsonPropertyName("data")]
        public MercadoPagoOrdenData? Data { get; set; }
    }


    // ============================================================
    // ESTADO ORDER
    // GET /api/pos/mercadopago/estado-orden
    // ============================================================

    public class MercadoPagoEstadoOrdenResponse
    {
        [JsonPropertyName("res")]
        public int Res { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("estado_incierto")]
        public bool EstadoIncierto { get; set; }

        [JsonPropertyName("requiere_conciliacion")]
        public bool RequiereConciliacion { get; set; }

        [JsonPropertyName("id_operacion")]
        public long? IdOperacionError { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderIdError { get; set; }

        [JsonPropertyName("http_status_mp")]
        public int? HttpStatusMercadoPago { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("data")]
        public MercadoPagoOrdenData? Data { get; set; }
    }


    // ============================================================
    // CANCELAR ORDER
    // POST /api/pos/mercadopago/cancelar-orden
    // ============================================================

    public class MercadoPagoCancelarOrdenRequest
    {
        [JsonPropertyName("id_operacion")]
        public long IdOperacion { get; set; }
    }


    public class MercadoPagoCancelarOrdenResponse
    {
        [JsonPropertyName("res")]
        public int Res { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("requiere_reembolso")]
        public bool RequiereReembolso { get; set; }

        [JsonPropertyName("requiere_recuperacion")]
        public bool RequiereRecuperacion { get; set; }

        [JsonPropertyName("requiere_sincronizacion")]
        public bool RequiereSincronizacion { get; set; }

        [JsonPropertyName("requiere_verificacion")]
        public bool RequiereVerificacion { get; set; }

        [JsonPropertyName("requiere_accion_terminal")]
        public bool RequiereAccionTerminal { get; set; }

        [JsonPropertyName("cancelar_desde_terminal")]
        public bool CancelarDesdeTerminal { get; set; }

        [JsonPropertyName("estado_incierto")]
        public bool EstadoIncierto { get; set; }

        [JsonPropertyName("id_operacion")]
        public long? IdOperacionError { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderIdError { get; set; }

        [JsonPropertyName("http_status_mp")]
        public int? HttpStatusMercadoPago { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("data")]
        public MercadoPagoOrdenData? Data { get; set; }
    }


    // ============================================================
    // INFORMACIÓN COMÚN DE UNA ORDER
    // ============================================================

    public class MercadoPagoOrdenData
    {
        [JsonPropertyName("id_operacion")]
        public long IdOperacion { get; set; }

        [JsonPropertyName("id_venta")]
        public long IdVenta { get; set; }

        [JsonPropertyName("folio")]
        public string? Folio { get; set; }

        [JsonPropertyName("id_tpv_bancaria")]
        public int IdTpvBancaria { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        [JsonPropertyName("payment_id")]
        public string? PaymentId { get; set; }

        [JsonPropertyName("external_reference")]
        public string? ExternalReference { get; set; }

        [JsonPropertyName("importe")]
        public decimal Importe { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("status_detail")]
        public string? StatusDetail { get; set; }

        [JsonPropertyName("estado_novacore")]
        public string? EstadoNovaCore { get; set; }

        [JsonPropertyName("payment_method_type")]
        public string? PaymentMethodType { get; set; }

        [JsonPropertyName("payment_method_id")]
        public string? PaymentMethodId { get; set; }

        [JsonPropertyName("id_forma_pago")]
        public int? IdFormaPago { get; set; }

        [JsonPropertyName("id_pago_pos")]
        public long? IdPagoPos { get; set; }

        [JsonPropertyName("pago_registrado")]
        public bool PagoRegistrado { get; set; }

        [JsonPropertyName("pago_completo")]
        public bool PagoCompleto { get; set; }

        [JsonPropertyName("termino_sin_pago")]
        public bool TerminoSinPago { get; set; }

        [JsonPropertyName("cancelada")]
        public bool Cancelada { get; set; }

        [JsonPropertyName("requiere_recuperacion")]
        public bool RequiereRecuperacion { get; set; }

        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        [JsonPropertyName("monto_pagado")]
        public decimal? MontoPagado { get; set; }

        [JsonPropertyName("saldo")]
        public decimal? Saldo { get; set; }
    }


    // ============================================================
    // RESULTADO INTERNO DEL PROCESO DE COBRO
    // ============================================================
    //
    // Esta clase NO corresponde directamente a un endpoint.
    //
    // La utilizaremos desde MercadoPagoService para entregar
    // a VentasViewModel un resultado limpio después de todo el
    // proceso de:
    //
    // crear order
    //      ↓
    // polling
    //      ↓
    // acreditación / rechazo / cancelación
    //
    // Así VentasViewModel NO necesita conocer detalles internos
    // de la API de Mercado Pago.
    // ============================================================

    public class MercadoPagoResultadoCobro
    {
        public bool Exitoso { get; set; }

        public bool PagoRegistrado { get; set; }

        public bool PagoCompleto { get; set; }

        public bool Cancelado { get; set; }

        public bool Rechazado { get; set; }

        public bool Expirado { get; set; }

        public bool RequiereConciliacion { get; set; }

        public bool RequiereAccionTerminal { get; set; }

        public long? IdOperacion { get; set; }

        public long? IdPagoPos { get; set; }

        public string? OrderId { get; set; }

        public string? PaymentId { get; set; }

        public string? Estado { get; set; }

        public string? Mensaje { get; set; }

        public decimal Importe { get; set; }
    }
}