using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Tickets;

namespace NovaCoreESDM.Services.WhatsApp;

public class WhatsAppService
{
    public Task<bool> AbrirWhatsAppAsync(
        TicketVenta ticket,
        string telefono)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(telefono))
            {
                return Task.FromResult(false);
            }

            var telefonoLimpio =
                LimpiarTelefono(
                    telefono
                );

            if (string.IsNullOrWhiteSpace(telefonoLimpio))
            {
                return Task.FromResult(false);
            }


            var mensaje =
                ConstruirMensaje(
                    ticket
                );


            var mensajeCodificado =
                Uri.EscapeDataString(
                    mensaje
                );


            // =================================================
            // 1) APLICACIÓN DE ESCRITORIO DE WHATSAPP
            // =================================================
            //
            // Si está instalada, se abre directo en la app (no
            // necesita navegador).

            var urlApp =
                $"whatsapp://send?phone={telefonoLimpio}&text={mensajeCodificado}";

            if (TieneWhatsAppEscritorio() &&
                IntentarAbrir(urlApp))
            {
                return Task.FromResult(true);
            }


            // =================================================
            // 2) NAVEGADOR PREDETERMINADO (Edge, Chrome, etc.)
            // =================================================

            var urlWeb =
                $"https://wa.me/{telefonoLimpio}?text={mensajeCodificado}";

            if (IntentarAbrir(urlWeb))
            {
                return Task.FromResult(true);
            }


            // =================================================
            // 3) WINDOWS SIN NAVEGADOR PREDETERMINADO
            // =================================================
            //
            // Explorer abre la liga con el navegador disponible
            // (Edge viene incluido en Windows).

            if (OperatingSystem.IsWindows() &&
                IntentarAbrirConProceso("explorer.exe", $"\"{urlWeb}\""))
            {
                return Task.FromResult(true);
            }

            if (OperatingSystem.IsWindows() &&
                IntentarAbrirConProceso("msedge.exe", $"\"{urlWeb}\""))
            {
                return Task.FromResult(true);
            }


            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"No fue posible abrir WhatsApp: {ex.Message}"
            );

            return Task.FromResult(false);
        }
    }


    // =========================================================
    // ABRIR LIGAS
    // =========================================================

    private static bool IntentarAbrir(
        string url)
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        url,

                    UseShellExecute =
                        true
                }
            );

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"No fue posible abrir {url[..Math.Min(url.Length, 24)]}…: {ex.Message}"
            );

            return false;
        }
    }


    private static bool IntentarAbrirConProceso(
        string programa,
        string argumentos)
    {
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        programa,

                    Arguments =
                        argumentos,

                    UseShellExecute =
                        true
                }
            );

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"No fue posible abrir WhatsApp con {programa}: {ex.Message}"
            );

            return false;
        }
    }


    // =========================================================
    // ¿ESTÁ INSTALADO WHATSAPP DE ESCRITORIO?
    // =========================================================
    //
    // La app registra el protocolo "whatsapp:". Solo se usa si
    // existe; si no, Windows mostraría "buscar una aplicación".
    // =========================================================

    private static bool TieneWhatsAppEscritorio()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using var clave =
                Microsoft.Win32.Registry.ClassesRoot
                    .OpenSubKey("whatsapp");

            return clave is not null;
        }
        catch
        {
            return false;
        }
    }


    // =========================================================
    // CONSTRUIR MENSAJE
    // =========================================================

    private static string ConstruirMensaje(
        TicketVenta ticket)
    {
        var sb =
            new StringBuilder();


        sb.AppendLine(
            "GRUPO LUIS FER"
        );

        sb.AppendLine();

        sb.AppendLine(
            "¡Gracias por su compra!"
        );

        sb.AppendLine();

        sb.AppendLine(
            $"Ticket: {ticket.FolioTicket}"
        );

        sb.AppendLine(
            $"Fecha: {ticket.Fecha:dd/MM/yyyy HH:mm}"
        );

        sb.AppendLine(
            $"Total: ${ticket.Total:N2}"
        );

        sb.AppendLine();

        sb.AppendLine(
            "Forma de pago:"
        );

        sb.AppendLine(
            ticket.FormaPago
        );

        sb.AppendLine();

        sb.AppendLine(
            "Consulta nuestra plataforma:"
        );

        sb.AppendLine(
            ticket.ContenidoQr
        );


        return
            sb.ToString();
    }


    // =========================================================
    // LIMPIAR TELÉFONO
    // =========================================================

    private static string LimpiarTelefono(
        string telefono)
    {
        var limpio =
            new StringBuilder();


        foreach (var caracter in telefono)
        {
            if (char.IsDigit(caracter))
            {
                limpio.Append(
                    caracter
                );
            }
        }


        var numero =
            limpio.ToString();


        /*
         * Si estás trabajando en México y
         * el usuario captura solamente 10 dígitos:
         *
         * 5512345678
         *
         * WhatsApp necesita:
         *
         * 525512345678
         */

        if (numero.Length == 10)
        {
            numero =
                "52" +
                numero;
        }


        return
            numero;
    }
}