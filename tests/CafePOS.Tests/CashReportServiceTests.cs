using CafePOS.Application.Interfaces;
using CafePOS.Application.Services;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using Xunit;

namespace CafePOS.Tests;

public sealed class CashReportServiceTests
{
    [Fact]
    public async Task Daily_report_uses_full_payments_and_excludes_cancelled_orders()
    {
        var day = new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc);
        var paidOrder = Order(day.AddHours(10), OrderStatus.Paid, 100m,
            [new Payment { Method = PaymentMethod.Cash, Amount = 100m, BusinessDate = "2026-08-13" }]);
        var openOrder = Order(day.AddHours(11), OrderStatus.Open, 50m, Array.Empty<Payment>());
        var cancelled = Order(day.AddHours(12), OrderStatus.Cancelled, 80m,
        [
            new Payment { Method = PaymentMethod.Cash, Amount = 80m, BusinessDate = "2026-08-13" },
            new Payment { Method = PaymentMethod.Cash, Amount = 80m, Kind = PaymentKind.Refund, BusinessDate = "2026-08-13" }
        ]);
        var transactions = new[]
        {
            new ManualTransaction { Concept = "Fondo", Amount = 30m, Type = ManualTransactionType.Income, CreatedAt = day.AddHours(9) },
            new ManualTransaction { Concept = "Limpieza", Amount = 5m, Type = ManualTransactionType.Expense, CreatedAt = day.AddHours(9) }
        };

        var report = await new CashReportService(
            new FakeOrderRepository([paidOrder, openOrder, cancelled]),
            new FakeManualTransactionRepository(transactions)).GetDailyAsync(day);

        Assert.Equal(2, report.Orders);
        Assert.Equal(50m, report.OutstandingOrders);
        Assert.Equal(100m, report.CashOrders);
        Assert.Equal(0m, report.CardOrders);
        Assert.Equal(125m, report.TotalCash);
        Assert.Equal(125m, report.TotalInCaja);
        Assert.Single(report.CategorySales);
    }

    private static Order Order(DateTime createdAt, OrderStatus status, decimal amount, IReadOnlyList<Payment> payments) => new()
    {
        CreatedAt = createdAt,
        Status = status,
        Items = [new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Latte", Category = ProductCategory.Coffee, Quantity = 1, UnitPrice = amount }],
        Payments = payments.ToList()
    };

    private sealed class FakeOrderRepository(IReadOnlyList<Order> values) : IOrderRepository
    {
        public Task<IReadOnlyList<Order>> GetOrdersAsync(DateTime from, DateTime to, string? search, CancellationToken ct = default) =>
            Task.FromResult(values.Where(x => x.CreatedAt >= from && x.CreatedAt < to).ToList() as IReadOnlyList<Order>);
        public Task<IReadOnlyList<Payment>> GetPaymentsAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
            Task.FromResult(values.SelectMany(x => x.Payments).ToList() as IReadOnlyList<Payment>);
        public Task<Order> SaveOrderAsync(Order order, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Order?> GetOrderAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Order> ReplacePaymentsAsync(Guid orderId, IReadOnlyList<Payment> payments, string reason, Guid employeeId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Order> CancelOrderAsync(Guid orderId, string reason, Guid employeeId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Order> UpdateDeliveryAsync(Guid orderId, DeliveryStatus status, string riderName, DateTime? promisedAt, bool cashOnDelivery, Guid employeeId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeManualTransactionRepository(IReadOnlyList<ManualTransaction> values) : IManualTransactionRepository
    {
        public Task<IReadOnlyList<ManualTransaction>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(values);
        public Task<IReadOnlyList<ManualTransaction>> GetForPeriodAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
            Task.FromResult(values.Where(x => x.CreatedAt >= from && x.CreatedAt < to).ToList() as IReadOnlyList<ManualTransaction>);
        public Task<ManualTransaction> AddAsync(ManualTransaction transaction, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ManualTransaction> ReverseAsync(Guid id, string reason, Guid employeeId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
