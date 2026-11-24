using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NovaCoreESDM.Models.Productos;

public class CatalogoProductosResponse
{
    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("Status")]
    public int Status { get; set; }

    [JsonPropertyName("Data")]
    public List<ProductoCatalogo> Data { get; set; } = new();

    /// <summary>True cuando no hubo internet y se usó la copia guardada en la caja.</summary>
    [JsonIgnore]
    public bool DesdeCopiaLocal { get; set; }

    /// <summary>Cuándo se guardó la copia local que se está usando.</summary>
    [JsonIgnore]
    public System.DateTime? FechaCopiaLocal { get; set; }
}