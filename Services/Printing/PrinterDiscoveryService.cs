using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NovaCoreESDM.Services.Printing;

/// <summary>
/// Lee las impresoras instaladas en Windows (las mismas que aparecen en
/// "Impresoras y escáneres") usando el spooler, sin paquetes extra.
/// </summary>
public class PrinterDiscoveryService
{
    public bool EsCompatible =>
        OperatingSystem.IsWindows();

    private const int PRINTER_ENUM_LOCAL = 0x00000002;
    private const int PRINTER_ENUM_CONNECTIONS = 0x00000004;


    [StructLayout(LayoutKind.Sequential)]
    private struct PRINTER_INFO_4
    {
        public IntPtr pPrinterName;
        public IntPtr pServerName;
        public uint Attributes;
    }


    [DllImport(
        "winspool.drv",
        EntryPoint = "EnumPrintersW",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern bool EnumPrinters(
        int flags,
        string? name,
        int level,
        IntPtr pPrinterEnum,
        int cbBuf,
        out int pcbNeeded,
        out int pcReturned);


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


    /// <summary>
    /// Nombres de todas las impresoras instaladas, ordenados A-Z.
    /// </summary>
    public List<string> ObtenerImpresoras()
    {
        var resultado = new List<string>();

        if (!EsCompatible)
            return resultado;

        const int flags =
            PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS;

        // 1ª llamada: preguntamos cuánta memoria hace falta.
        EnumPrinters(
            flags, null, 4,
            IntPtr.Zero, 0,
            out var bytesNecesarios,
            out _);

        if (bytesNecesarios <= 0)
            return resultado;

        var buffer = Marshal.AllocHGlobal(bytesNecesarios);

        try
        {
            // 2ª llamada: ahora sí pedimos la lista.
            if (!EnumPrinters(
                    flags, null, 4,
                    buffer, bytesNecesarios,
                    out _,
                    out var cantidad))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Windows no pudo listar las impresoras.");
            }

            var tamano = Marshal.SizeOf<PRINTER_INFO_4>();

            for (var i = 0; i < cantidad; i++)
            {
                var actual =
                    IntPtr.Add(buffer, i * tamano);

                var info =
                    Marshal.PtrToStructure<PRINTER_INFO_4>(actual);

                var nombre =
                    Marshal.PtrToStringUni(info.pPrinterName);

                if (!string.IsNullOrWhiteSpace(nombre))
                    resultado.Add(nombre);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        resultado.Sort(StringComparer.CurrentCultureIgnoreCase);

        return resultado;
    }


    /// <summary>
    /// true si Windows puede abrir una impresora con ese nombre.
    /// (No significa que esté encendida: significa que existe su cola.)
    /// </summary>
    public bool ImpresoraExiste(string nombre)
    {
        if (!EsCompatible || string.IsNullOrWhiteSpace(nombre))
            return false;

        if (!OpenPrinter(nombre, out var handle, IntPtr.Zero))
            return false;

        ClosePrinter(handle);

        return true;
    }
}
