using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Ventas;

// =============================================================
// CHG-0005 fase 2: promociones configurables (ERP → Promociones).
// GET  api/pos/ventas/{id}/promociones                 → cuáles aplican
// POST api/pos/ventas/{id}/promociones/{id}/aplicar    → con autorización
// =============================================================

public class PromocionesVentaResponse
{
    [JsonPropertyName("res")] public int Res { get; set; }
    [JsonPropertyName("msg")] public string? Msg { get; set; }
    [JsonPropertyName("data")] public PromocionesVentaData? Data { get; set; }
}

public class PromocionesVentaData
{
    [JsonPropertyName("promociones")] public List<PromocionVenta> Promociones { get; set; } = new();
}

public class PromocionVenta
{
    [JsonPropertyName("id_promocion")] public int IdPromocion { get; set; }
    [JsonPropertyName("nombre")] public string? Nombre { get; set; }
    [JsonPropertyName("descripcion")] public string? Descripcion { get; set; }
    [JsonPropertyName("tipo")] public string? Tipo { get; set; }
    [JsonPropertyName("veces")] public int Veces { get; set; }
    [JsonPropertyName("ahorro")] public decimal Ahorro { get; set; }
    [JsonPropertyName("aplicada")] public bool Aplicada { get; set; }
    [JsonPropertyName("veces_aplicadas")] public int VecesAplicadas { get; set; }
    [JsonPropertyName("puede_aplicar")] public bool PuedeAplicar { get; set; }
    [JsonPropertyName("motivo")] public string? Motivo { get; set; }

    [JsonIgnore]
    public string TextoTitulo => string.IsNullOrWhiteSpace(Nombre) ? "Promoción" : Nombre!;

    [JsonIgnore]
    public string TextoDetalle =>
        PuedeAplicar
            ? $"{Descripcion} · El cliente ahorra ${Ahorro:N2}"
            : string.IsNullOrWhiteSpace(Motivo) ? Descripcion ?? string.Empty : $"{Descripcion} · {Motivo}";

    [JsonIgnore]
    public string TextoBoton => Veces > 1 ? $"Aplicar ×{Veces}" : "Aplicar";
}

public class AplicarPromocionResponse
{
    [JsonPropertyName("res")] public int Res { get; set; }
    [JsonPropertyName("msg")] public string? Msg { get; set; }
    [JsonPropertyName("codigo")] public string? Codigo { get; set; }
    [JsonPropertyName("data")] public AplicarPromocionData? Data { get; set; }
}

public class AplicarPromocionData
{
    [JsonPropertyName("id_promocion")] public int IdPromocion { get; set; }
    [JsonPropertyName("nombre")] public string? Nombre { get; set; }
    [JsonPropertyName("veces")] public int Veces { get; set; }
    [JsonPropertyName("ahorro")] public decimal Ahorro { get; set; }
}
