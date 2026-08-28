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
}