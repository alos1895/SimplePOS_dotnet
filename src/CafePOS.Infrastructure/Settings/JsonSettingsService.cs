using System.Text.Json; using CafePOS.Application.Interfaces;
namespace CafePOS.Infrastructure.Settings;
public sealed class JsonSettingsService : ISettingsService
{
 private readonly IDataPathProvider _paths; public AppSettings Current {get;private set;}
 public JsonSettingsService(IDataPathProvider paths) { _paths=paths; try { Current=File.Exists(paths.SettingsPath)?JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(paths.SettingsPath))??new():new(); if(Current.StoreName=="CafePOS")Current=Current with {StoreName="Gloria Café"}; } catch { Current=new(); } }
 public async Task SaveAsync(AppSettings settings,CancellationToken ct=default) { Directory.CreateDirectory(_paths.DataDirectory); var temp=_paths.SettingsPath+".tmp"; await File.WriteAllTextAsync(temp,JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}),ct); File.Move(temp,_paths.SettingsPath,true); Current=settings; }
}
