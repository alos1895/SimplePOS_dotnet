using CafePOS.Domain.Enums;

namespace CafePOS.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BusinessDate { get; set; } = Services.BusinessDate.Today;
    public int DailyNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Comments { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public Guid? DeliveryOptionId { get; set; }
    public string DeliveryOptionName { get; set; } = "";
    public DeliveryType DeliveryType { get; set; } = DeliveryType.Pickup;
    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.None;
    public decimal DeliveryFee { get; set; }
    public string DeliveryAddress { get; set; } = "";
    public string RiderName { get; set; } = "";
    public DateTime? PromisedAt { get; set; }
    public bool CashOnDelivery { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];

    public decimal Subtotal => Items.Sum(x => x.LineTotal);
    public decimal Total => Subtotal + DeliveryFee;
    public decimal PaidAmount => Payments.Sum(x => x.NetAmount);
    public decimal BalanceDue => Math.Max(0m, Total - PaidAmount);
    public bool IsFullyPaid => Total > 0 && PaidAmount == Total;
    public IReadOnlyList<Payment> CurrentCollections => Payments
        .Where(x => x.Kind == PaymentKind.Collection &&
                    !Payments.Any(reversal => reversal.Kind is PaymentKind.Reversal or PaymentKind.Refund &&
                                              reversal.ReversesPaymentId == x.Id))
        .OrderBy(x => x.CreatedAt)
        .ToList();
}

public sealed class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public required string ProductName { get; set; }
    public ProductCategory Category { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public string BusinessDate { get; set; } = Services.BusinessDate.Today;
    public Guid EmployeeId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentKind Kind { get; set; } = PaymentKind.Collection;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public Guid? ReversesPaymentId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal NetAmount => Kind == PaymentKind.Collection ? Amount : -Amount;
}
