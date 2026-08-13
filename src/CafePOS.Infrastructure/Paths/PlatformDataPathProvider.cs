using CafePOS.Application.Interfaces;
namespace CafePOS.Infrastructure.Paths;
public sealed class PlatformDataPathProvider : IDataPathProvider
{
 public string DataDirectory { get; }
 public string DatabasePath=>Path.Combine(DataDirectory,"cafe.db"); public string BackupDirectory=>Path.Combine(DataDirectory,"backups"); public string LogDirectory=>Path.Combine(DataDirectory,"logs"); public string SettingsPath=>Path.Combine(DataDirectory,"settings.json");
 public PlatformDataPathProvider() { DataDirectory=OperatingSystem.IsWindows()?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"CafePOS"):OperatingSystem.IsMacOS()?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),"Library","Application Support","CafePOS"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CafePOS"); }
}
