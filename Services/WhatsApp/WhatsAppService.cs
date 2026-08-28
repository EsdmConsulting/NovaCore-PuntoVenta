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


            var url =
                $"https://wa.me/{telefonoLimpio}?text={mensajeCodificado}";


            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        url,

                    UseShellExecute =
                        true
                }
            );


            return Task.FromResult(true);
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