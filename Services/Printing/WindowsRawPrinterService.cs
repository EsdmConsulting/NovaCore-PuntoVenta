using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NovaCoreESDM.Services.Printing;

public class WindowsRawPrinterService
{
    // =========================================================
    // CONFIGURACIÓN
    // =========================================================

    private const string NombreImpresora =
        "POS-grupoluisfer";


    // =========================================================
    // ESTRUCTURA DOCUMENTO WINDOWS
    // =========================================================

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private class DOC_INFO_1
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDocName =
            string.Empty;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pOutputFile;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDataType =
            "RAW";
    }


    // =========================================================
    // WINDOWS PRINT SPOOLER
    // =========================================================

    [DllImport(
        "winspool.drv",
        EntryPoint = "OpenPrinterW",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(
        string pPrinterName,
        out IntPtr phPrinter,
        IntPtr pDefault);


    [DllImport(
        "winspool.drv",
        SetLastError = true)]
    private static extern bool ClosePrinter(
        IntPtr hPrinter);


    [DllImport(
        "winspool.drv",
        EntryPoint = "StartDocPrinterW",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern int StartDocPrinter(
        IntPtr hPrinter,
        int level,
        [In] DOC_INFO_1 docInfo);


    [DllImport(
        "winspool.drv",
        SetLastError = true)]
    private static extern bool EndDocPrinter(
        IntPtr hPrinter);


    [DllImport(
        "winspool.drv",
        SetLastError = true)]
    private static extern bool StartPagePrinter(
        IntPtr hPrinter);


    [DllImport(
        "winspool.drv",
        SetLastError = true)]
    private static extern bool EndPagePrinter(
        IntPtr hPrinter);


    [DllImport(
        "winspool.drv",
        SetLastError = true)]
    private static extern bool WritePrinter(
        IntPtr hPrinter,
        IntPtr pBytes,
        int dwCount,
        out int dwWritten);
    
    
    
    


    // =========================================================
    // IMPRESIÓN GENÉRICA
    // =========================================================

    /*
     * Este método será utilizado por TicketVentaService.
     *
     * Recibe directamente los bytes ESC/POS
     * que queremos mandar a la impresora.
     */
    public Task<bool> ImprimirAsync(
        byte[] contenido,
        string nombreDocumento = "NovaCore - Ticket")
    {
        return Task.Run(
            () =>
                ImprimirRaw(
                    NombreImpresora,
                    contenido,
                    nombreDocumento
                )
        );
    }
    
    


    // =========================================================
    // IMPRESIÓN DE PRUEBA
    // =========================================================

    public Task<bool> ImprimirPruebaAsync()
    {
        return Task.Run(
            () =>
            {
                var contenido =
                    ConstruirPrueba();

                return ImprimirRaw(
                    NombreImpresora,
                    contenido,
                    "NovaCore - Prueba ESC/POS"
                );
            });
    }
    
    
    
    // =========================================================
    // ABRIR CAJÓN DE EFECTIVO
    // =========================================================

    public Task<bool> AbrirCajonAsync()
    {
        return Task.Run(
            () =>
            {
                /*
                 * ESC/POS:
                 *
                 * ESC p m t1 t2
                 *
                 * 0x1B = ESC
                 * 0x70 = p
                 * 0x00 = pin / conector 0
                 * 0x19 = duración del pulso ON
                 * 0xFA = duración del pulso OFF
                 *
                 * IMPORTANTE:
                 * Este comando NO imprime.
                 *
                 * Únicamente le pide a la impresora
                 * que envíe el pulso eléctrico
                 * al puerto del cajón.
                 */

                var comandoAbrirCajon =
                    new byte[]
                    {
                        0x1B,
                        0x70,
                        0x00,
                        0x19,
                        0xFA
                    };


                return ImprimirRaw(
                    NombreImpresora,
                    comandoAbrirCajon,
                    "Grupo Luis Fer - Abrir cajon"
                );
            });
    }


    // =========================================================
    // CONSTRUIR TICKET DE PRUEBA
    // =========================================================

    private static byte[] ConstruirPrueba()
    {
        var bytes =
            new List<byte>();


        // Inicializar ESC/POS
        bytes.AddRange(
            new byte[]
            {
                0x1B,
                0x40
            });
        

        // =====================================================
        // CENTRAR
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1B,
                0x61,
                0x01
            });


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "NOVA CORE\n"));


        // =====================================================
        // NEGRITAS ON
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1B,
                0x45,
                0x01
            });


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "PRUEBA DE IMPRESION\n"));


        // =====================================================
        // NEGRITAS OFF
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1B,
                0x45,
                0x00
            });


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "\nPOS-grupoluisfer\n"));


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "Impresora POS-8360-L\n"));


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "80 mm - USB\n\n"));


        // =====================================================
        // ALINEAR IZQUIERDA
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1B,
                0x61,
                0x00
            });


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "------------------------------------------\n"));


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "Si puedes leer este ticket,\n"));


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "NovaCore ya se comunica con la impresora.\n"));


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "------------------------------------------\n\n"));


        // =====================================================
        // CENTRAR NUEVAMENTE
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1B,
                0x61,
                0x01
            });


        bytes.AddRange(
            Encoding.ASCII.GetBytes(
                "PRUEBA EXITOSA\n\n\n"));


        // =====================================================
        // CORTE PARCIAL
        // =====================================================

        bytes.AddRange(
            new byte[]
            {
                0x1D,
                0x56,
                0x01
            });


        return bytes.ToArray();
    }


    // =========================================================
    // ENVIAR DATOS RAW A WINDOWS
    // =========================================================

    private static bool ImprimirRaw(
        string printerName,
        byte[] bytes,
        string nombreDocumento)
    {
        IntPtr hPrinter =
            IntPtr.Zero;

        IntPtr unmanagedBytes =
            IntPtr.Zero;

        var documentoIniciado =
            false;

        var paginaIniciada =
            false;


        try
        {
            // =================================================
            // ABRIR IMPRESORA
            // =================================================

            if (
                !OpenPrinter(
                    printerName,
                    out hPrinter,
                    IntPtr.Zero)
            )
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    $"No fue posible abrir la impresora '{printerName}'.");
            }


            // =================================================
            // INFORMACIÓN DEL DOCUMENTO
            // =================================================

            var docInfo =
                new DOC_INFO_1
                {
                    pDocName =
                        nombreDocumento,

                    pDataType =
                        "RAW"
                };


            // =================================================
            // INICIAR DOCUMENTO
            // =================================================

            var job =
                StartDocPrinter(
                    hPrinter,
                    1,
                    docInfo);


            if (job == 0)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "No fue posible iniciar el documento de impresión.");
            }


            documentoIniciado =
                true;


            // =================================================
            // INICIAR PÁGINA
            // =================================================

            if (!StartPagePrinter(hPrinter))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "No fue posible iniciar la página de impresión.");
            }


            paginaIniciada =
                true;


            // =================================================
            // COPIAR BYTES
            // =================================================

            unmanagedBytes =
                Marshal.AllocCoTaskMem(
                    bytes.Length);


            Marshal.Copy(
                bytes,
                0,
                unmanagedBytes,
                bytes.Length);


            // =================================================
            // ENVIAR A IMPRESORA
            // =================================================

            if (
                !WritePrinter(
                    hPrinter,
                    unmanagedBytes,
                    bytes.Length,
                    out var bytesWritten)
            )
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "No fue posible enviar datos a la impresora.");
            }


            return
                bytesWritten ==
                bytes.Length;
        }
        finally
        {
            // =================================================
            // CERRAR PÁGINA
            // =================================================

            if (
                paginaIniciada &&
                hPrinter != IntPtr.Zero
            )
            {
                EndPagePrinter(
                    hPrinter);
            }


            // =================================================
            // CERRAR DOCUMENTO
            // =================================================

            if (
                documentoIniciado &&
                hPrinter != IntPtr.Zero
            )
            {
                EndDocPrinter(
                    hPrinter);
            }


            // =================================================
            // LIBERAR MEMORIA
            // =================================================

            if (
                unmanagedBytes !=
                IntPtr.Zero
            )
            {
                Marshal.FreeCoTaskMem(
                    unmanagedBytes);
            }


            // =================================================
            // CERRAR IMPRESORA
            // =================================================

            if (
                hPrinter !=
                IntPtr.Zero
            )
            {
                ClosePrinter(
                    hPrinter);
            }
        }
    }
}