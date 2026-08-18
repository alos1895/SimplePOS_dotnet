using CafePOS.Domain.Enums;

namespace CafePOS.Application.Models;

public enum AdminPage { Home, Products }

public sealed record AdminProductItem(
    Guid Id,
    string Name,
    ProductCategory Category,
    decimal Price,
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
    decimal Price);

public sealed record DeliveryOptionUpsert(
    Guid? Id,
    string Name,
    DeliveryType Type,
    decimal Fee);

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
    IReadOnlyList<CategoryMetric> BottomCategories)
{
    public decimal NetSales => CollectedPayments;
    public static BusinessMetrics Empty { get; } = new(
        0, 0, 0, 0, 0, 0, new PeriodComparison(0, 0, 0, 0, 0, 0, 0, 0), [], [], [], []);
}
