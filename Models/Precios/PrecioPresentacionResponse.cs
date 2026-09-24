using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Precios;

public class PrecioPresentacionResponse
{
    [JsonPropertyName("res")]
    public int Res { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public PrecioPresentacionData? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class PrecioPresentacionData
{
    [JsonPropertyName("presentacion")]
    public PresentacionPrecioInfo? Presentacion { get; set; }

    [JsonPropertyName("precios")]
    public List<PrecioVigentePresentacion> Precios { get; set; } = new();

    [JsonPropertyName("reglas")]
    public List<ReglaPrecioPresentacion> Reglas { get; set; } = new();

    [JsonPropertyName("tipos_disponibles")]
    public List<TipoPrecioPresentacion> TiposDisponibles { get; set; } = new();

    [JsonPropertyName("nivel_base")]
    public TipoPrecioPresentacion? NivelBase { get; set; }

    [JsonPropertyName("configuracion_valida")]
    public bool ConfiguracionValida { get; set; }

    [JsonPropertyName("reglas_incompletas")]
    public List<ReglaIncompletaPresentacion> ReglasIncompletas { get; set; } = new();

    [JsonPropertyName("cantidad_consultada")]
    public decimal? CantidadConsultada { get; set; }

    [JsonPropertyName("nivel_automatico")]
    public TipoPrecioPresentacion? NivelAutomatico { get; set; }

    [JsonPropertyName("siguiente_nivel")]
    public SiguienteNivelPresentacion? SiguienteNivel { get; set; }
}

public class PresentacionPrecioInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("id_producto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("producto_codigo")]
    public string ProductoCodigo { get; set; } = string.Empty;

    [JsonPropertyName("producto_nombre")]
    public string ProductoNombre { get; set; } = string.Empty;

    [JsonPropertyName("nombre_presentacion")]
    public string NombrePresentacion { get; set; } = string.Empty;

    [JsonPropertyName("unidad_medida")]
    public string UnidadMedida { get; set; } = string.Empty;

    [JsonPropertyName("factor_conversion")]
    public decimal FactorConversion { get; set; }

    [JsonPropertyName("codigo_barras")]
    public string CodigoBarras { get; set; } = string.Empty;

    [JsonPropertyName("es_presentacion_base")]
    public bool EsPresentacionBase { get; set; }
}

public class PrecioVigentePresentacion
{
    [JsonPropertyName("id_precio")]
    public int IdPrecio { get; set; }

    [JsonPropertyName("tipo_precio")]
    public string TipoPrecio { get; set; } = string.Empty;

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }

    [JsonPropertyName("fecha_inicio")]
    public string FechaInicio { get; set; } = string.Empty;

    [JsonPropertyName("fecha_fin")]
    public string? FechaFin { get; set; }

    [JsonPropertyName("precio_valido")]
    public bool PrecioValido { get; set; }
}

public class ReglaPrecioPresentacion
{
    [JsonPropertyName("id_regla")]
    public int IdRegla { get; set; }

    [JsonPropertyName("tipo_precio")]
    public string TipoPrecio { get; set; } = string.Empty;

    [JsonPropertyName("cantidad_minima")]
    public decimal CantidadMinima { get; set; }

    [JsonPropertyName("id_precio")]
    public int? IdPrecio { get; set; }

    [JsonPropertyName("precio")]
    public decimal? Precio { get; set; }

    [JsonPropertyName("disponible")]
    public bool Disponible { get; set; }
}

public class TipoPrecioPresentacion
{
    [JsonPropertyName("id_regla")]
    public int IdRegla { get; set; }

    [JsonPropertyName("id_precio")]
    public int IdPrecio { get; set; }

    [JsonPropertyName("tipo_precio")]
    public string TipoPrecio { get; set; } = string.Empty;

    [JsonPropertyName("cantidad_minima")]
    public decimal CantidadMinima { get; set; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }
}

public class ReglaIncompletaPresentacion
{
    [JsonPropertyName("id_regla")]
    public int IdRegla { get; set; }

    [JsonPropertyName("tipo_precio")]
    public string TipoPrecio { get; set; } = string.Empty;

    [JsonPropertyName("cantidad_minima")]
    public decimal CantidadMinima { get; set; }

    [JsonPropertyName("motivo")]
    public string Motivo { get; set; } = string.Empty;
}

public class SiguienteNivelPresentacion
{
    [JsonPropertyName("id_regla")]
    public int IdRegla { get; set; }

    [JsonPropertyName("id_precio")]
    public int IdPrecio { get; set; }

    [JsonPropertyName("tipo_precio")]
    public string TipoPrecio { get; set; } = string.Empty;

    [JsonPropertyName("cantidad_minima")]
    public decimal CantidadMinima { get; set; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }

    [JsonPropertyName("faltan")]
    public decimal Faltan { get; set; }

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; } = string.Empty;
}