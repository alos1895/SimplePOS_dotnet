using CafePOS.Application.Interfaces;
using CafePOS.Application.Models;
using CafePOS.Domain.Enums;
using CafePOS.Domain.Services;

namespace CafePOS.Application.Services;

public sealed class BusinessMetricsService(
    IOrderRepository orders,
    ICurrentUserContext? user = null)
{
    public async Task<BusinessMetrics> GetAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (user is not null && user.Current.Role != EmployeeRole.Admin)
            throw new UnauthorizedAccessException("Solo un administrador puede consultar indicadores.");
        var start = from.Date;
        var end = to.Date.AddDays(1);
        if (end <= start) throw new InvalidOperationException("El rango de métricas no es válido.");

        var periodLength = end - start;
        var previousStart = start - periodLength;
        var currentOrders = (await orders.GetOrdersAsync(start, end, null, ct))
            .Where(x => x.Status != OrderStatus.Cancelled).ToList();
        var previousOrders = (await orders.GetOrdersAsync(previousStart, start, null, ct))
            .Where(x => x.Status != OrderStatus.Cancelled).ToList();
        var currentPayments = await orders.GetPaymentsAsync(start, end, ct);
        var previousPayments = await orders.GetPaymentsAsync(previousStart, start, ct);

        var invoicedSales = currentOrders.Sum(x => x.Total);
        var previousInvoicedSales = previousOrders.Sum(x => x.Total);
        var averageTicket = currentOrders.Count == 0 ? 0 : invoicedSales / currentOrders.Count;
        var previousAverageTicket = previousOrders.Count == 0 ? 0 : previousInvoicedSales / previousOrders.Count;
        var trend = Enumerable.Range(0, periodLength.Days).Select(offset =>
        {
            var day = BusinessDate.FromLocalDate(start.AddDays(offset));
            var dayOrders = currentOrders.Where(x => x.BusinessDate == day).ToList();
            return new MetricDay(
                day,
                dayOrders.Count,
                dayOrders.Sum(x => x.Total),
                currentPayments.Where(x => x.BusinessDate == day).Sum(x => x.NetAmount),
                dayOrders.Sum(x => x.BalanceDue));
        }).ToList();

        var items = currentOrders.SelectMany(x => x.Items).ToList();
        var categoryMetrics = Enum.GetValues<ProductCategory>()
            .Select(category =>
            {
                var categoryItems = items.Where(x => x.Category == category);
                return new CategoryMetric(category, categoryItems.Sum(x => x.Quantity), categoryItems.Sum(x => x.LineTotal));
            })
            .ToList();
        var topProducts = items.GroupBy(x => x.ProductName)
            .Select(x => new ProductMetric(x.Key, x.Sum(i => i.Quantity), x.Sum(i => i.LineTotal)))
            .OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Sales).Take(10).ToList();

        return new BusinessMetrics(
            currentOrders.Count,
            invoicedSales,
            currentPayments.Sum(x => x.NetAmount),
            currentOrders.Sum(x => x.BalanceDue),
            averageTicket,
            currentOrders.Sum(x => x.DeliveryFee),
            new PeriodComparison(
                previousOrders.Count,
                previousInvoicedSales,
                previousPayments.Sum(x => x.NetAmount),
                previousAverageTicket,
                currentOrders.Count - previousOrders.Count,
                invoicedSales - previousInvoicedSales,
                currentPayments.Sum(x => x.NetAmount) - previousPayments.Sum(x => x.NetAmount),
                averageTicket - previousAverageTicket),
            trend,
            topProducts,
            categoryMetrics.OrderByDescending(x => x.Sales).ThenByDescending(x => x.Quantity).ToList(),
            categoryMetrics.OrderBy(x => x.Sales).ThenBy(x => x.Quantity).ToList());

    }
}
