using CafePOS.Application.Interfaces; using CafePOS.Infrastructure.Backups; using CafePOS.Infrastructure.Paths; using CafePOS.Infrastructure.Persistence; using CafePOS.Infrastructure.Printing; using CafePOS.Infrastructure.Settings; using CafePOS.Infrastructure.Updates; using Microsoft.EntityFrameworkCore; using Microsoft.Extensions.DependencyInjection;
namespace CafePOS.Infrastructure;
public static class DependencyInjection
{
 public static IServiceCollection AddCafePosInfrastructure(this IServiceCollection s)
 { s.AddSingleton<IDataPathProvider,PlatformDataPathProvider>();s.AddSingleton<ISettingsService,JsonSettingsService>();s.AddSingleton<IBackupService,SqliteBackupService>();s.AddSingleton<IReceiptPrinter,DebugReceiptPrinter>();s.AddSingleton<IAppInitializer,AppInitializer>();s.AddSingleton<IPosStore,PosStore>();s.AddHttpClient<IUpdateService,GitHubReleaseUpdateService>();s.AddDbContextFactory<CafePosDbContext>((sp,o)=>{var p=sp.GetRequiredService<IDataPathProvider>();o.UseSqlite($"Data Source={p.DatabasePath};Foreign Keys=True");});return s; }
}
