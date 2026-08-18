using CafePOS.Domain.Enums;

namespace CafePOS.Application.Models;

public sealed record CategorySales(ProductCategory Category, int Quantity, decimal Revenue);

public sealed record CashDailyReport(
    int Orders,
    decimal OutstandingOrders,
    decimal CashOrders,
    decimal CardOrders,
    decimal DeliveryRevenue,
    decimal ManualIncome,
    decimal ManualExpenses,
    decimal TotalCash,
    decimal TotalInCaja,
    IReadOnlyList<CategorySales> CategorySales)
{
    public static CashDailyReport Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, []);
}
