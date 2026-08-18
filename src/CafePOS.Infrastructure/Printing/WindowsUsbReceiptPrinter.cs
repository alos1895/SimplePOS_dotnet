using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using CafePOS.Application.Interfaces;

namespace CafePOS.Infrastructure.Printing;

public sealed class WindowsUsbReceiptPrinter : IReceiptPrinter
{
    public bool IsSupported => OperatingSystem.IsWindows();
    public Task<IReadOnlyList<string>> GetInstalledPrintersAsync(CancellationToken ct = default) =>
        !IsSupported ? Task.FromResult<IReadOnlyList<string>>([]) : Task.Run<IReadOnlyList<string>>(EnumeratePrinters, ct);
    public Task PrintAsync(string printerName, string documentName, string content, CancellationToken ct = default)
    {
        if (!IsSupported) throw new PlatformNotSupportedException("La impresión USB está disponible en Windows.");
        if (string.IsNullOrWhiteSpace(printerName)) throw new InvalidOperationException("Seleccione una impresora.");
        return Task.Run(() => WriteRaw(printerName, documentName, content), ct);
    }

    private static IReadOnlyList<string> EnumeratePrinters()
    {
        const uint flags = 2 | 4;
        EnumPrinters(flags, null, 4, IntPtr.Zero, 0, out var needed, out _);
        if (needed == 0) return [];
        var buffer = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!EnumPrinters(flags, null, 4, buffer, needed, out _, out var count)) ThrowLastError("No se pudieron consultar las impresoras");
            var size = Marshal.SizeOf<PrinterInfo4>();
            var names = new List<string>((int)count);
            for (uint i = 0; i < count; i++)
            {
                var info = Marshal.PtrToStructure<PrinterInfo4>(IntPtr.Add(buffer, checked((int)i * size)));
                var name = Marshal.PtrToStringUni(info.PrinterName);
                if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
            }
            return names.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void WriteRaw(string printerName, string documentName, string content)
    {
        if (!OpenPrinter(printerName, out var printer, IntPtr.Zero)) ThrowLastError($"No se pudo abrir '{printerName}'");
        try
        {
            var doc = new DocInfo { DocumentName = documentName, DataType = "RAW" };
            if (StartDocPrinter(printer, 1, ref doc) == 0) ThrowLastError("No se pudo iniciar el trabajo de impresión");
            try
            {
                if (!StartPagePrinter(printer)) ThrowLastError("No se pudo iniciar la página");
                var bytes = Encoding.ASCII.GetBytes(RemoveDiacritics(content));
                var data = new byte[2 + bytes.Length + 4];
                data[0] = 0x1B; data[1] = 0x40;
                Buffer.BlockCopy(bytes, 0, data, 2, bytes.Length);
                data[^4] = 0x1D; data[^3] = 0x56; data[^2] = 0x41; data[^1] = 0x03;
                var pointer = Marshal.AllocHGlobal(data.Length);
                try
                {
                    Marshal.Copy(data, 0, pointer, data.Length);
                    if (!WritePrinter(printer, pointer, data.Length, out var written) || written != data.Length) ThrowLastError("No se pudo enviar el ticket completo");
                }
                finally { Marshal.FreeHGlobal(pointer); }
                EndPagePrinter(printer);
            }
            finally { EndDocPrinter(printer); }
        }
        finally { ClosePrinter(printer); }
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        return new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    private static void ThrowLastError(string message) => throw new Win32Exception(Marshal.GetLastWin32Error(), message);
    [StructLayout(LayoutKind.Sequential)] private struct PrinterInfo4 { public IntPtr PrinterName; public IntPtr ServerName; public uint Attributes; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct DocInfo { [MarshalAs(UnmanagedType.LPWStr)] public required string DocumentName; [MarshalAs(UnmanagedType.LPWStr)] public string? OutputFile; [MarshalAs(UnmanagedType.LPWStr)] public required string DataType; }
    [DllImport("winspool.drv", EntryPoint = "EnumPrintersW", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool EnumPrinters(uint flags, string? name, uint level, IntPtr buffer, uint size, out uint needed, out uint returned);
    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool OpenPrinter(string name, out IntPtr printer, IntPtr defaults);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool ClosePrinter(IntPtr printer);
    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)] private static extern uint StartDocPrinter(IntPtr printer, uint level, ref DocInfo info);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool EndDocPrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool StartPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool EndPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool WritePrinter(IntPtr printer, IntPtr bytes, int count, out int written);
}
