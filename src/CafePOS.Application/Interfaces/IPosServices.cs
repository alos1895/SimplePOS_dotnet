using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
namespace CafePOS.Application.Interfaces;

public interface IPosStore
{
    Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetOrdersAsync(DateTimeOffset from, DateTimeOffset to, string? search, CancellationToken ct = default);
    Task<Order> SaveOrderAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetOrderAsync(Guid id, CancellationToken ct = default);
    Task<CashSession?> GetOpenCashSessionAsync(CancellationToken ct = default);
    Task<CashSession> SaveCashSessionAsync(CashSession session, CancellationToken ct = default);
}
public interface IDataPathProvider { string DataDirectory { get; } string DatabasePath { get; } string BackupDirectory { get; } string LogDirectory { get; } string SettingsPath { get; } }
public interface IBackupService { Task<string?> CreateAsync(string reason, CancellationToken ct = default); Task RotateAsync(CancellationToken ct = default); }
public interface IReceiptPrinter { Task PrintAsync(Order order, CancellationToken ct = default); }
public interface IAppInitializer { Task InitializeAsync(CancellationToken ct = default); }
public interface ISettingsService { AppSettings Current { get; } Task SaveAsync(AppSettings settings, CancellationToken ct = default); }
public interface IUpdateService { Task<UpdateInfo?> CheckAsync(CancellationToken ct = default); Task<string> DownloadAsync(UpdateInfo update, CancellationToken ct = default); }
public sealed record UpdateInfo(Version Version, Uri DownloadUri, string FileName);
public sealed record AppSettings(string StoreName = "CafePOS", string Currency = "MXN", string TicketFooter = "¡Gracias por su compra!", int RegisterNumber = 1, string? Printer = null, string? GitHubRepository = null, bool CheckUpdates = true);
