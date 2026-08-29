using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using NovaCoreESDM.Services.Api;


namespace NovaCoreESDM.Services.Productos;


public class ProductoImagenService
{
    public async Task<Bitmap?> CargarImagenAsync(
        Uri? imagenUrl)
    {
        if (imagenUrl is null)
            return null;


        try
        {
            using var response =
                await ApiClient.Http
                    .GetAsync(
                        imagenUrl
                    );


            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"Imagen no disponible: {imagenUrl} - {(int)response.StatusCode}");

                return null;
            }


            var bytes =
                await response.Content
                    .ReadAsByteArrayAsync();


            if (bytes.Length == 0)
                return null;


            using var stream =
                new MemoryStream(
                    bytes
                );


            return new Bitmap(
                stream
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"No fue posible cargar imagen {imagenUrl}: {ex.Message}");

            return null;
        }
    }
}