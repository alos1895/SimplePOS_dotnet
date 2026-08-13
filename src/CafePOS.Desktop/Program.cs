using Avalonia; using CafePOS.Application.Interfaces; using CafePOS.Application.Services; using CafePOS.Desktop; using CafePOS.Desktop.ViewModels; using CafePOS.Infrastructure; using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensions.Hosting; using Serilog;
internal static class Program
{
 public static IHost Host {get;private set;}=null!;
 [STAThread] public static void Main(string[] args)
 {
  var paths=new CafePOS.Infrastructure.Paths.PlatformDataPathProvider();Directory.CreateDirectory(paths.LogDirectory);
  Log.Logger=new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(Path.Combine(paths.LogDirectory,"cafepos-.log"),rollingInterval:RollingInterval.Day,retainedFileCountLimit:14).CreateLogger();
  Host=Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args).UseSerilog().ConfigureServices(s=>{s.AddCafePosInfrastructure();s.AddSingleton<CheckoutService>();s.AddSingleton<CashService>();s.AddSingleton<MainViewModel>();}).Build();
  try { Host.Services.GetRequiredService<IAppInitializer>().InitializeAsync().GetAwaiter().GetResult(); BuildAvaloniaApp().StartWithClassicDesktopLifetime(args); }
  catch(Exception ex) { Log.Fatal(ex,"CafePOS could not start");throw; } finally { Host.Dispose();Log.CloseAndFlush(); }
 }
 public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
