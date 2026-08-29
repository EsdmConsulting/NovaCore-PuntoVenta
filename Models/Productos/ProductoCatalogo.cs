using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using NovaCoreESDM.Services.Api;


namespace NovaCoreESDM.Models.Productos;


public partial class ProductoCatalogo : ObservableObject
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }


    [JsonPropertyName("Codigo")]
    public string Codigo { get; set; } =
        string.Empty;


    [JsonPropertyName("Sku")]
    public string Sku { get; set; } =
        string.Empty;


    [JsonPropertyName("CodigoBarras")]
    public string CodigoBarras { get; set; } =
        string.Empty;


    [JsonPropertyName("NombreComercial")]
    public string NombreComercial { get; set; } =
        string.Empty;


    [JsonPropertyName("Descripcion")]
    public string Descripcion { get; set; } =
        string.Empty;


    [JsonPropertyName("Categoria")]
    public string Categoria { get; set; } =
        string.Empty;


    [JsonPropertyName("Subcategoria")]
    public string Subcategoria { get; set; } =
        string.Empty;


    [JsonPropertyName("Familia")]
    public string Familia { get; set; } =
        string.Empty;


    [JsonPropertyName("Marca")]
    public string Marca { get; set; } =
        string.Empty;


    [JsonPropertyName("UnidadMedidaBase")]
    public string UnidadMedidaBase { get; set; } =
        string.Empty;


    [JsonPropertyName("AplicaInventario")]
    public bool AplicaInventario { get; set; }


    [JsonPropertyName("AplicaVenta")]
    public bool AplicaVenta { get; set; }


    [JsonPropertyName("AplicaCompra")]
    public bool AplicaCompra { get; set; }


    // =========================================================
    // RUTA DE IMAGEN DEVUELTA POR LA API
    // =========================================================

    [JsonPropertyName("ImagenRuta")]
    public string? ImagenRuta { get; set; }


    // =========================================================
    // URL COMPLETA DE LA IMAGEN
    // =========================================================

    [JsonIgnore]
    public Uri? ImagenUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(
                    ImagenRuta))
            {
                return null;
            }


            var baseAddress =
                ApiClient.Http.BaseAddress;


            if (baseAddress is null)
                return null;


            return new Uri(
                baseAddress,
                ImagenRuta.TrimStart('/')
            );
        }
    }


    // =========================================================
    // BITMAP CARGADO EN MEMORIA
    // =========================================================

    [ObservableProperty]
    [property: JsonIgnore]
    private Bitmap? _imagenBitmap;


    // =========================================================
    // ESTADO VISUAL DE LA IMAGEN
    // =========================================================

    [JsonIgnore]
    public bool TieneImagenCargada =>
        ImagenBitmap is not null;


    [JsonIgnore]
    public bool MostrarPlaceholderImagen =>
        ImagenBitmap is null;


    // =========================================================
    // NOTIFICAR CAMBIOS DE VISIBILIDAD
    // =========================================================

    partial void OnImagenBitmapChanged(
        Bitmap? value)
    {
        OnPropertyChanged(
            nameof(TieneImagenCargada));


        OnPropertyChanged(
            nameof(MostrarPlaceholderImagen));
    }


    [JsonPropertyName("Estatus")]
    public int Estatus { get; set; }


    [JsonPropertyName("Presentaciones")]
    public List<PresentacionProducto>
        Presentaciones { get; set; } =
            new();


    [JsonPropertyName("Disponibilidad")]
    public List<DisponibilidadProducto>
        Disponibilidad { get; set; } =
            new();
}