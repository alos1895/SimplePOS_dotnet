using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Domain.Services;
using Xunit;

namespace CafePOS.Tests;

public sealed class DomainRulesTests
{
    [Fact]
    public void Full_cash_payment_marks_order_paid_at_exact_total()
    {
        var order = OrderWithTotal(100m);
        var payments = new[] { new Payment { Method = PaymentMethod.Cash, Amount = 100m } };

        OrderRules.ValidateFullPayment(order, payments);
        order.Payments.AddRange(payments);

        Assert.True(order.IsFullyPaid);
        Assert.Equal(0m, order.BalanceDue);
    }

    [Fact]
    public void Full_payment_rejects_an_amount_different_from_order_total()
    {
        var order = OrderWithTotal(100m);
        Assert.Throws<InvalidOperationException>(() => OrderRules.ValidateFullPayment(order,
            [new Payment { Method = PaymentMethod.Cash, Amount = 100.01m }]));
    }

    private static Order OrderWithTotal(decimal total) => new()
    {
        EmployeeId = Guid.NewGuid(),
        Items = [new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Latte", Category = ProductCategory.Coffee, Quantity = 1, UnitPrice = total }]
    };
}
