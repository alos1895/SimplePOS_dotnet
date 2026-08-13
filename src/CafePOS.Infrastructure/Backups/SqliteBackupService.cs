using CafePOS.Application.Interfaces; using Microsoft.Data.Sqlite;
namespace CafePOS.Infrastructure.Backups;
public sealed class SqliteBackupService(IDataPathProvider paths) : IBackupService
{
 public async Task<string?> CreateAsync(string reason,CancellationToken ct=default) { if(!File.Exists(paths.DatabasePath))return null; Directory.CreateDirectory(paths.BackupDirectory); var target=Path.Combine(paths.BackupDirectory,$"cafe-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Sanitize(reason)}.db"); await using var source=new SqliteConnection($"Data Source={paths.DatabasePath};Mode=ReadOnly"); await using var destination=new SqliteConnection($"Data Source={target}"); await source.OpenAsync(ct); await destination.OpenAsync(ct); source.BackupDatabase(destination); await RotateAsync(ct); return target; }
 public Task RotateAsync(CancellationToken ct=default) { if(!Directory.Exists(paths.BackupDirectory))return Task.CompletedTask; foreach(var file in new DirectoryInfo(paths.BackupDirectory).GetFiles("cafe-*.db").OrderByDescending(x=>x.CreationTimeUtc).Skip(30)) file.Delete(); return Task.CompletedTask; }
 private static string Sanitize(string value)=>string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
}
