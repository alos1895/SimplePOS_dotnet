using CafePOS.Application.Models;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;

namespace CafePOS.Application.Interfaces;

public interface ICatalogRepository
{
    Task<IReadOnlyList<CatalogItem>> GetAvailableItemsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DeliveryOptionItem>> GetDeliveryOptionsAsync(CancellationToken ct = default);
}

public interface IAdminCatalogRepository
{
    Task<AdminCatalogData> GetAsync(CancellationToken ct = default);
    Task SaveProductAsync(ProductUpsert product, CancellationToken ct = default);
    Task DeactivateProductAsync(Guid id, CancellationToken ct = default);
    Task SaveDeliveryOptionAsync(DeliveryOptionUpsert option, CancellationToken ct = default);
    Task DeactivateDeliveryOptionAsync(Guid id, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetOrdersAsync(DateTime from, DateTime to, string? search, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetPaymentsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Order> SaveOrderAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetOrderAsync(Guid id, CancellationToken ct = default);
    Task<Order> ReplacePaymentsAsync(Guid orderId, IReadOnlyList<Payment> payments, string reason, Guid employeeId, CancellationToken ct = default);
    Task<Order> CancelOrderAsync(Guid orderId, string reason, Guid employeeId, CancellationToken ct = default);
    Task<Order> UpdateDeliveryAsync(Guid orderId, DeliveryStatus status, string riderName, DateTime? promisedAt, bool cashOnDelivery, Guid employeeId, CancellationToken ct = default);
}

public interface IManualTransactionRepository
{
    Task<IReadOnlyList<ManualTransaction>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ManualTransaction>> GetForPeriodAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<ManualTransaction> AddAsync(ManualTransaction transaction, CancellationToken ct = default);
    Task<ManualTransaction> ReverseAsync(Guid id, string reason, Guid employeeId, CancellationToken ct = default);
}

public sealed record CurrentUser(Guid EmployeeId, string DisplayName, EmployeeRole Role);

public interface ICurrentUserContext
{
    CurrentUser Current { get; }
}

public interface IDataPathProvider
{
    string DataDirectory { get; }
    string DatabasePath { get; }
    string BackupDirectory { get; }
    string LogDirectory { get; }
    string SettingsPath { get; }
}

public interface IBackupService
{
    Task<string?> CreateAsync(string reason, CancellationToken ct = default);
    Task RotateAsync(CancellationToken ct = default);
}

public interface IAppInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}

public interface ISettingsService
{
    AppSettings Current { get; }
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}

public interface IReceiptPrinter
{
    bool IsSupported { get; }
    Task<IReadOnlyList<string>> GetInstalledPrintersAsync(CancellationToken ct = default);
    Task PrintAsync(string printerName, string documentName, string content, CancellationToken ct = default);
}

public interface IUpdateService
{
    Task<UpdateInfo?> CheckAsync(CancellationToken ct = default);
    Task<string> DownloadAsync(UpdateInfo update, CancellationToken ct = default);
}

public interface IUpdateInstaller
{
    Task StageAndRestartAsync(string zipPath, CancellationToken ct = default);
}

public sealed record UpdateInfo(Version Version, Uri DownloadUri, string FileName);
public sealed record AppSettings(string StoreName = "CafePOS", string Currency = "MXN", int RegisterNumber = 1,
    string? GitHubRepository = "alos1895/SimplePOS_dotnet", bool CheckUpdates = true,
    string KitchenPrinter = "", string CustomerPrinter = "",
    bool AutoPrintKitchen = false, bool AutoPrintCustomer = false);
