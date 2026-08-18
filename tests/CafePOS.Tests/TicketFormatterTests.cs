using CafePOS.Application.Interfaces;
using CafePOS.Application.Services;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using Xunit;

namespace CafePOS.Tests;

public sealed class TicketFormatterTests
{
    [Fact]
    public void Kitchen_ticket_contains_order_items_and_instructions()
    {
        var formatter = new TicketFormatter(new TestSettings());
        var ticket = formatter.Kitchen(CreateOrder());

        Assert.Contains("COMANDA COCINA", ticket);
        Assert.Contains("ORDEN #18", ticket);
        Assert.Contains("2 x Latte", ticket);
        Assert.Contains("Sin azúcar", ticket);
        var lines = ticket.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.All(lines, line => Assert.True(line.Length <= 32, $"La línea excede 32 caracteres: '{line}'"));
    }

    [Fact]
    public void Customer_ticket_contains_store_totals_and_payment()
    {
        var formatter = new TicketFormatter(new TestSettings());
        var ticket = formatter.Customer(CreateOrder());

        Assert.Contains("Café Prueba", ticket);
        Assert.Contains("TICKET DE VENTA", ticket);
        Assert.Contains("TOTAL:", ticket);
        Assert.Contains("MXN 90.00", ticket);
        Assert.DoesNotContain("XDR", ticket);
        Assert.Contains("Pago: Efectivo", ticket);
        Assert.Contains(new string('-', 32), ticket);
    }

    private static Order CreateOrder() => new()
    {
        DailyNumber = 18,
        CustomerName = "Ana",
        DeliveryOptionName = "Recoge en tienda",
        Comments = "Sin azúcar",
        Items = [new OrderItem { ProductName = "Latte", Category = ProductCategory.Coffee, UnitPrice = 45m, Quantity = 2 }],
        Payments = [new Payment { Amount = 90m, Method = PaymentMethod.Cash }]
    };

    private sealed class TestSettings : ISettingsService
    {
        public AppSettings Current { get; } = new(StoreName: "Café Prueba");
        public Task SaveAsync(AppSettings settings, CancellationToken ct = default) => Task.CompletedTask;
    }
}
