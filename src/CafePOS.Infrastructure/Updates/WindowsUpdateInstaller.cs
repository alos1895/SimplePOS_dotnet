using System.Diagnostics;
using System.IO.Compression;
using CafePOS.Application.Interfaces;

namespace CafePOS.Infrastructure.Updates;

public sealed class WindowsUpdateInstaller : IUpdateInstaller
{
    public Task StageAndRestartAsync(string zipPath, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("La instalación automática solo está disponible en Windows.");
        if (!File.Exists(zipPath))
            throw new FileNotFoundException("No se encontró el paquete de actualización.", zipPath);

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("No se pudo determinar la ubicación de CafePOS.");
        var installDirectory = Path.GetDirectoryName(executablePath)
            ?? throw new InvalidOperationException("No se pudo determinar el directorio de CafePOS.");
        var stagingDirectory = Path.Combine(Path.GetTempPath(), $"CafePOS-update-{Guid.NewGuid():N}");

        Directory.CreateDirectory(stagingDirectory);
        ExtractPackage(zipPath, stagingDirectory, ct);

        var stagedExecutable = Path.Combine(stagingDirectory, Path.GetFileName(executablePath));
        if (!File.Exists(stagedExecutable))
        {
            Directory.Delete(stagingDirectory, true);
            throw new InvalidDataException($"El paquete no contiene {Path.GetFileName(executablePath)}.");
        }

        var scriptPath = Path.Combine(Path.GetTempPath(), $"CafePOS-update-{Guid.NewGuid():N}.ps1");
        File.WriteAllText(scriptPath, BuildUpdaterScript(Environment.ProcessId, stagingDirectory, installDirectory, executablePath));

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File {Quote(scriptPath)}",
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("No se pudo iniciar el instalador de la actualización.");

        return Task.CompletedTask;
    }

    private static void ExtractPackage(string zipPath, string destination, CancellationToken ct)
    {
        var destinationRoot = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();
            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!target.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("El paquete de actualización contiene una ruta no válida.");

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(target);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, true);
        }
    }

    private static string BuildUpdaterScript(int processId, string stagingDirectory, string installDirectory, string executablePath) =>
        $$"""
        $ErrorActionPreference = 'Stop'
        try {
            Wait-Process -Id {{processId}} -Timeout 60 -ErrorAction SilentlyContinue
            Get-ChildItem -LiteralPath {{PowerShellLiteral(stagingDirectory)}} | Copy-Item -Destination {{PowerShellLiteral(installDirectory)}} -Recurse -Force
            Start-Process -FilePath {{PowerShellLiteral(executablePath)}} -WorkingDirectory {{PowerShellLiteral(installDirectory)}}
        }
        finally {
            Remove-Item -LiteralPath {{PowerShellLiteral(stagingDirectory)}} -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue
        }
        """;

    private static string PowerShellLiteral(string value) => $"'{value.Replace("'", "''")}'";
    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
}
