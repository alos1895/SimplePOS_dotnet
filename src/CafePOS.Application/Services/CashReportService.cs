using CafePOS.Application.Interfaces;
using CafePOS.Application.Models;
using CafePOS.Domain.Enums;

namespace CafePOS.Application.Services;

public sealed class CashReportService(IOrderRepository orders, IManualTransactionRepository transactions)
{
    public async Task<CashDailyReport> GetDailyAsync(DateTime day, CancellationToken ct = default)
    {
        var start = day.Date;
        var end = start.AddDays(1);
        var dailyOrders = (await orders.GetOrdersAsync(start, end, null, ct))
            .Where(x => x.Status != OrderStatus.Cancelled).ToList();
        var dailyTransactions = await transactions.GetForPeriodAsync(start, end, ct);
        var payments = await orders.GetPaymentsAsync(start, end, ct);
        var manualIncome = dailyTransactions.Where(x => x.Type == ManualTransactionType.Income).Sum(x => x.Amount);
        var manualExpenses = dailyTransactions.Where(x => x.Type == ManualTransactionType.Expense).Sum(x => x.Amount);
        var cash = payments.Where(x => x.Method == PaymentMethod.Cash).Sum(x => x.NetAmount);
        var card = payments.Where(x => x.Method == PaymentMethod.Card).Sum(x => x.NetAmount);
        var categorySales = dailyOrders.SelectMany(x => x.Items)
            .GroupBy(x => x.Category)
            .Select(x => new CategorySales(x.Key, x.Sum(i => i.Quantity), x.Sum(i => i.LineTotal)))
            .OrderBy(x => x.Category)
            .ToList();

        return new CashDailyReport(
            dailyOrders.Count,
            dailyOrders.Where(x => x.Status == OrderStatus.Open).Sum(x => x.BalanceDue),
            cash,
            card,
            dailyOrders.Sum(x => x.DeliveryFee),
            manualIncome,
            manualExpenses,
            cash + manualIncome - manualExpenses,
            cash + card + manualIncome - manualExpenses,
            categorySales);
    }
}
