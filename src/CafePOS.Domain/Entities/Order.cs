using CafePOS.Domain.Enums;

namespace CafePOS.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int DailyNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Comments { get; set; } = "";
    public decimal Discount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public decimal Subtotal => Items.Sum(x => x.LineTotal);
    public decimal Total => Math.Max(0m, Subtotal - Discount);
    public decimal PaidAmount => Payments.Sum(x => x.Amount);
    public decimal BalanceDue => Math.Max(0m, Total - PaidAmount);
    public bool IsFullyPaid => PaidAmount >= Total && Total > 0;
}

public sealed class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid OrderId { get; set; }
    public Order? Order { get; set; } public Guid? ProductId { get; set; }
    public Product? Product { get; set; } public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; } public int Quantity { get; set; }
    public string? Notes { get; set; } public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid OrderId { get; set; }
    public Order? Order { get; set; } public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; } public string? Reference { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
