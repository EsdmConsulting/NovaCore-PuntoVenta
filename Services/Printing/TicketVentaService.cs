using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using NovaCoreESDM.Models.Configuration;
using NovaCoreESDM.Models.Tickets;
using NovaCoreESDM.Services.Configuration;
using SkiaSharp;

namespace NovaCoreESDM.Services.Printing;

public class TicketVentaService
{
    private readonly WindowsRawPrinterService
        _printerService = new();

    private readonly TicketFormatConfigurationService
        _formatoService = new();


    // =========================================================
    // IMPRIMIR UN TICKET (venta nueva o reimpresión)
    // =========================================================

    public async Task<bool> ImprimirAsync(
        TicketVenta ticket)
    {
        var formato =
            _formatoService.Obtener();

        var contenido =
            ConstruirTicket(ticket, formato);

        var nombreDocumento =
            ticket.EsReimpresion
                ? $"Reimpresion {ticket.FolioTicket}"
                : $"Ticket {ticket.FolioTicket}";

        return await _printerService
            .ImprimirAsync(
                contenido,
                nombreDocumento
            );
    }


    // =========================================================
    // TICKET DE MUESTRA (para probar el formato sin hacer una venta)
    // =========================================================

    public Task<bool> ImprimirMuestraAsync() =>
        ImprimirAsync(TicketVenta.CrearMuestra());


    // =========================================================
    // CONSTRUIR EL TICKET
    // =========================================================

    private static byte[] ConstruirTicket(
        TicketVenta ticket,
        TicketFormatConfiguration formato)
    {
        var bytes = new List<byte>();
        var ancho = formato.AnchoCaracteres;

        // Inicializar impresora
        bytes.AddRange(new byte[] { 0x1B, 0x40 });


        // =====================================================
        // LOGO
        // =====================================================

        if (formato.MostrarLogo)
        {
            Centrar(bytes);
            AgregarLogo(bytes);
            AgregarTexto(bytes, "\n");
        }


        // =====================================================
        // ENCABEZADO
        // =====================================================

        Centrar(bytes);
        Negrita(bytes, true);

        foreach (var linea in formato.Encabezado)
            AgregarTexto(bytes, linea + "\n");

        Negrita(bytes, false);

        if (formato.MostrarUnidadOperativa &&
            !string.IsNullOrWhiteSpace(ticket.UnidadOperativa))
        {
            AgregarTexto(bytes, ticket.UnidadOperativa + "\n");
        }

        AgregarTexto(bytes, "\n");


        // =====================================================
        // AVISO DE REIMPRESIÓN
        // =====================================================

        if (ticket.EsReimpresion)
        {
            Negrita(bytes, true);
            AgregarTexto(bytes, "*** REIMPRESION ***\n");
            Negrita(bytes, false);

            var fechaReimpresion =
                ticket.FechaReimpresion ?? DateTime.Now;

            AgregarTexto(
                bytes,
                $"Reimpreso: {fechaReimpresion:dd/MM/yyyy HH:mm}\n");

            if (!string.IsNullOrWhiteSpace(ticket.ReimpresoPor))
                AgregarTexto(bytes, $"Autorizo: {ticket.ReimpresoPor}\n");

            AgregarTexto(bytes, "\n");
        }


        // =====================================================
        // DATOS DE LA VENTA
        // =====================================================

        Izquierda(bytes);
        Separador(bytes, ancho);

        AgregarTexto(bytes, $"Ticket: {ticket.FolioTicket}\n");
        AgregarTexto(bytes, $"Fecha: {ticket.Fecha:dd/MM/yyyy HH:mm}\n");
        AgregarTexto(bytes, $"Caja: {ticket.Caja}\n");

        if (formato.MostrarCajero)
            AgregarTexto(bytes, $"Cajero: {ticket.Cajero}\n");

        Separador(bytes, ancho);


        // =====================================================
        // PRODUCTOS
        // =====================================================

        Negrita(bytes, true);
        AgregarTexto(bytes, "PRODUCTOS\n");
        Negrita(bytes, false);
        AgregarTexto(bytes, "\n");

        foreach (var producto in ticket.Productos)
        {
            AgregarTexto(bytes, $"{producto.Producto}\n");

            if (!string.IsNullOrWhiteSpace(producto.Presentacion))
                AgregarTexto(bytes, $"{producto.Presentacion}\n");

            var cantidad =
                producto.Cantidad.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture);

            var izquierda =
                $"{cantidad} x ${producto.PrecioUnitario:N2}";

            AgregarTexto(
                bytes,
                AlinearDosColumnas(
                    izquierda,
                    $"${producto.Importe:N2}",
                    ancho));

            AgregarTexto(bytes, "\n");
        }


        // =====================================================
        // TOTALES
        // =====================================================

        Separador(bytes, ancho);

        AgregarTexto(
            bytes,
            AlinearDosColumnas("SUBTOTAL:", $"${ticket.Subtotal:N2}", ancho));

        if (formato.MostrarImpuestos)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("IVA:", $"${ticket.Iva:N2}", ancho));

            if (ticket.Ieps > 0)
            {
                AgregarTexto(
                    bytes,
                    AlinearDosColumnas("IEPS:", $"${ticket.Ieps:N2}", ancho));
            }
        }

        Negrita(bytes, true);

        AgregarTexto(
            bytes,
            AlinearDosColumnas("TOTAL:", $"${ticket.Total:N2}", ancho));

        Negrita(bytes, false);

        SeparadorDoble(bytes, ancho);


        // =====================================================
        // FORMA DE PAGO
        // =====================================================

        Negrita(bytes, true);
        AgregarTexto(bytes, "FORMA DE PAGO\n");
        Negrita(bytes, false);

        var hayDesglose =
            ticket.Efectivo > 0 ||
            ticket.Tarjeta > 0 ||
            ticket.Transferencia > 0;

        if (ticket.Efectivo > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Efectivo:", $"${ticket.Efectivo:N2}", ancho));
        }

        if (ticket.Tarjeta > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Tarjeta:", $"${ticket.Tarjeta:N2}", ancho));
        }

        if (ticket.Transferencia > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Transferencia:", $"${ticket.Transferencia:N2}", ancho));
        }

        // Reimpresiones: si el servidor no entrega el desglose,
        // se imprime solo el monto pagado.
        if (!hayDesglose && ticket.Pagado > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Pagado:", $"${ticket.Pagado:N2}", ancho));
        }

        if (ticket.Recibido > 0 && ticket.Efectivo > 0)
        {
            AgregarTexto(
                bytes,
                AlinearDosColumnas("Recibido:", $"${ticket.Recibido:N2}", ancho));

            AgregarTexto(
                bytes,
                AlinearDosColumnas("Cambio:", $"${ticket.Cambio:N2}", ancho));
        }


        // =====================================================
        // PIE
        // =====================================================

        Separador(bytes, ancho);
        Centrar(bytes);
        AgregarTexto(bytes, "\n");

        if (!string.IsNullOrWhiteSpace(formato.MensajeFinal))
        {
            Negrita(bytes, true);
            AgregarTexto(bytes, formato.MensajeFinal + "\n");
            Negrita(bytes, false);
            AgregarTexto(bytes, "\n");
        }

        foreach (var linea in formato.Pie)
            AgregarTexto(bytes, linea + "\n");

        AgregarTexto(bytes, $"Ticket:\n{ticket.FolioTicket}\n\n");


        // =====================================================
        // QR
        // =====================================================

        if (formato.MostrarQr &&
            !string.IsNullOrWhiteSpace(ticket.ContenidoQr))
        {
            if (!string.IsNullOrWhiteSpace(formato.TextoQr))
                AgregarTexto(bytes, formato.TextoQr + "\n\n");

            AgregarQr(bytes, ticket.ContenidoQr);


           /*  if (!string.IsNullOrWhiteSpace(formato.TextoAviso))
                AgregarTexto(bytes, formato.TextoAviso + "\n\n"); */

            AgregarTexto(bytes, "\n\n");
        }


        AgregarTexto(
            bytes,
            new string('\n', Math.Max(0, formato.LineasEnBlancoAlFinal)));


        // Corte parcial
        if (formato.CortarPapel)
            bytes.AddRange(new byte[] { 0x1D, 0x56, 0x01 });


        return bytes.ToArray();
    }


    // =========================================================
    // HELPERS
    // =========================================================

    /// <summary>
    /// La impresora recibe ASCII. Quitamos los acentos (á -> a)
    /// para que no salgan signos "?" en el ticket.
    /// </summary>
    private static string Sanitizar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        var descompuesto =
            texto.Normalize(NormalizationForm.FormD);

        var sb =
            new StringBuilder(descompuesto.Length);

        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c)
                == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }


    private static void AgregarTexto(
        List<byte> bytes,
        string texto)
    {
        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                Sanitizar(texto)));
    }


    private static void Centrar(List<byte> bytes)
    {
        bytes.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
    }


    private static void Izquierda(List<byte> bytes)
    {
        bytes.AddRange(new byte[] { 0x1B, 0x61, 0x00 });
    }


    private static void Negrita(List<byte> bytes, bool activa)
    {
        bytes.AddRange(
            new byte[]
            {
                0x1B,
                0x45,
                activa ? (byte)0x01 : (byte)0x00
            });
    }


    private static void Separador(List<byte> bytes, int ancho)
    {
        AgregarTexto(bytes, new string('-', ancho) + "\n");
    }


    private static void SeparadorDoble(List<byte> bytes, int ancho)
    {
        AgregarTexto(bytes, new string('=', ancho) + "\n");
    }


    private static string AlinearDosColumnas(
        string izquierda,
        string derecha,
        int ancho)
    {
        izquierda = Sanitizar(izquierda);
        derecha = Sanitizar(derecha);

        var espacios =
            ancho - izquierda.Length - derecha.Length;

        if (espacios < 1)
        {
            // Si no cabe, recortamos el texto de la izquierda.
            var maximo =
                Math.Max(0, ancho - derecha.Length - 1);

            if (izquierda.Length > maximo)
                izquierda = izquierda.Substring(0, maximo);

            espacios = 1;
        }

        return izquierda
               + new string(' ', espacios)
               + derecha
               + "\n";
    }


    private static void AgregarQr(
        List<byte> bytes,
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return;

        var datos =
            Encoding.UTF8.GetBytes(contenido);

        // =====================================================
        // MODELO QR
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                0x04, 0x00,
                0x31, 0x41,
                0x32, 0x00
            });


        // =====================================================
        // TAMAÑO QR
        // 1 - 16
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                0x03, 0x00,
                0x31, 0x43,
                0x06
            });


        // =====================================================
        // CORRECCIÓN DE ERROR
        // 0x31 = Nivel M
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                0x03, 0x00,
                0x31, 0x45,
                0x31
            });


        // =====================================================
        // GUARDAR INFORMACIÓN DEL QR
        // =====================================================

        var longitud =
            datos.Length + 3;

        var pL =
            (byte)(longitud & 0xFF);

        var pH =
            (byte)((longitud >> 8) & 0xFF);


        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                pL, pH,
                0x31, 0x50, 0x30
            });


        bytes.AddRange(
            datos);


        // =====================================================
        // IMPRIMIR QR
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1D, 0x28, 0x6B,
                0x03, 0x00,
                0x31, 0x51, 0x30
            });
    }
    
    
    
    // =========================================================
// LOGO GRUPO LUISFER
// =========================================================

private static void AgregarLogo(
    List<byte> bytes)
{
    try
    {
        var uri =
            new Uri(
                "avares://NovaCoreESDM/Assets/logo_grupo_luisfer.png"
            );

        using var stream =
            AssetLoader.Open(uri);

        using var original =
            SKBitmap.Decode(stream);

        if (original is null)
        {
            Console.WriteLine(
                "No fue posible decodificar el logo."
            );

            return;
        }


        // =====================================================
        // REDIMENSIONAR
        // =====================================================

        const int anchoLogo =
            360;

        var proporcion =
            (double)anchoLogo /
            original.Width;

        var altoLogo =
            Math.Max(
                1,
                (int)Math.Round(
                    original.Height *
                    proporcion
                )
            );


        var info =
            new SKImageInfo(
                anchoLogo,
                altoLogo,
                SKColorType.Rgba8888,
                SKAlphaType.Premul
            );


        using var redimensionada =
            original.Resize(
                info,
                SKFilterQuality.High
            );


        if (redimensionada is null)
        {
            Console.WriteLine(
                "No fue posible redimensionar el logo."
            );

            return;
        }


        // =====================================================
        // CONVERTIR A RASTER ESC/POS
        // =====================================================

        var ancho =
            redimensionada.Width;

        var alto =
            redimensionada.Height;

        var bytesPorFila =
            (ancho + 7) / 8;


        // GS v 0
        bytes.AddRange(
            new byte[]
            {
                0x1D,
                0x76,
                0x30,
                0x00
            }
        );


        // Ancho en bytes
        bytes.Add(
            (byte)(
                bytesPorFila &
                0xFF
            )
        );

        bytes.Add(
            (byte)(
                (
                    bytesPorFila >>
                    8
                ) &
                0xFF
            )
        );


        // Alto en puntos
        bytes.Add(
            (byte)(
                alto &
                0xFF
            )
        );

        bytes.Add(
            (byte)(
                (
                    alto >>
                    8
                ) &
                0xFF
            )
        );


        // =====================================================
        // PIXELES
        // =====================================================

        for (
            var y = 0;
            y < alto;
            y++
        )
        {
            for (
                var byteX = 0;
                byteX < bytesPorFila;
                byteX++
            )
            {
                byte valor =
                    0;


                for (
                    var bit = 0;
                    bit < 8;
                    bit++
                )
                {
                    var x =
                        (byteX * 8)
                        + bit;


                    if (x >= ancho)
                        continue;


                    var pixel =
                        redimensionada.GetPixel(
                            x,
                            y
                        );


                    // Transparencia = blanco
                    if (pixel.Alpha < 20)
                        continue;


                    // Convertir RGB a luminosidad
                    var luminosidad =
                        (
                            pixel.Red * 0.299
                            +
                            pixel.Green * 0.587
                            +
                            pixel.Blue * 0.114
                        );


                    /*
                     * Threshold:
                     *
                     * menor = negro
                     * mayor = blanco
                     *
                     * 210 conserva bastante bien
                     * amarillos/verdes del logo.
                     */
                    var esNegro =
                        luminosidad < 210;


                    if (esNegro)
                    {
                        valor |=
                            (byte)(
                                0x80 >>
                                bit
                            );
                    }
                }


                bytes.Add(
                    valor
                );
            }
        }


        AgregarTexto(
            bytes,
            "\n"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"No fue posible cargar el logo del ticket: {ex.Message}"
        );
    }
}


}