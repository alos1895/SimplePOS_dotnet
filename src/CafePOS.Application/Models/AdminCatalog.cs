using CafePOS.Domain.Enums;

namespace CafePOS.Application.Models;

public enum AdminPage { Home, Products, Inventory, Metrics }

public sealed record AdminProductItem(
    Guid Id,
    string Name,
    ProductCategory Category,
    decimal Price,
    int StockQuantity,
    int LowStockThreshold,
    bool IsActive);

public sealed record AdminDeliveryOptionItem(
    Guid Id,
    string Name,
    DeliveryType Type,
    decimal Fee,
    bool IsActive);

public sealed record AdminCatalogData(
    IReadOnlyList<AdminProductItem> Products,
    IReadOnlyList<AdminDeliveryOptionItem> DeliveryOptions);

public sealed record ProductUpsert(
    Guid? Id,
    string Name,
    ProductCategory Category,
    decimal Price,
    int InitialStockQuantity,
    int LowStockThreshold);

public sealed record DeliveryOptionUpsert(
    Guid? Id,
    string Name,
    DeliveryType Type,
    decimal Fee);

public sealed record ProductStockItem(
    Guid Id,
    string Name,
    ProductCategory Category,
    int StockQuantity,
    int LowStockThreshold,
    bool IsActive)
{
    public bool IsLowStock => IsActive && StockQuantity <= LowStockThreshold;
}

public sealed record InventoryMovementItem(
    Guid Id,
    Guid ProductId,
    string ProductName,
    InventoryMovementType Type,
    int QuantityDelta,
    string Reason,
    string? Supplier,
    string? Reference,
    string BusinessDate,
    DateTime OccurredAt)
{
    public string Notes => Reason;
}

public sealed record InventorySnapshot(
    IReadOnlyList<ProductStockItem> Products,
    IReadOnlyList<InventoryMovementItem> Movements);

public sealed record StockAdjustment(
    Guid ProductId,
    InventoryMovementType Type,
    int Quantity,
    string Reason,
    string? Supplier,
    string? Reference,
    DateTime EffectiveDate,
    Guid EmployeeId);

public sealed record MetricDay(string BusinessDate, int Orders, decimal InvoicedSales, decimal CollectedPayments, decimal OutstandingBalance);
public sealed record ProductMetric(string Name, int Quantity, decimal Sales);
public sealed record CategoryMetric(ProductCategory Category, int Quantity, decimal Sales);
public sealed record PeriodComparison(
    int PreviousOrders,
    decimal PreviousInvoicedSales,
    decimal PreviousCollectedPayments,
    decimal PreviousAverageTicket,
    decimal OrdersChange,
    decimal InvoicedSalesChange,
    decimal CollectedPaymentsChange,
    decimal AverageTicketChange)
{
    public decimal PreviousNetSales => PreviousCollectedPayments;
    public decimal NetSalesChange => CollectedPaymentsChange;
}
public sealed record StockSignals(int Incoming, int Counted, int Waste, int Corrected, int Consumed, int Restored)
{
    public int Produced => Incoming;
    public int Adjusted => Corrected;
}

public sealed record BusinessMetrics(
    int Orders,
    decimal InvoicedSales,
    decimal CollectedPayments,
    decimal OutstandingBalance,
    decimal AverageTicket,
    decimal DeliveryRevenue,
    PeriodComparison Comparison,
    IReadOnlyList<MetricDay> Trend,
    IReadOnlyList<ProductMetric> TopProducts,
    IReadOnlyList<CategoryMetric> TopCategories,
    IReadOnlyList<CategoryMetric> BottomCategories,
    IReadOnlyList<ProductStockItem> LowStockAlerts,
    StockSignals StockSignals)
{
    public decimal NetSales => CollectedPayments;
    public static BusinessMetrics Empty { get; } = new(
        0, 0, 0, 0, 0, 0, new PeriodComparison(0, 0, 0, 0, 0, 0, 0, 0), [], [], [], [], [], new StockSignals(0, 0, 0, 0, 0, 0));
}
