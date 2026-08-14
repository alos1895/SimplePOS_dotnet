using CafePOS.Application.Interfaces;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Domain.Services;
namespace CafePOS.Application.Services;

public sealed class CheckoutService(IPosStore store, IReceiptPrinter printer)
{
    public async Task<Order> CreateOrderAndPayAsync(Order order, PaymentMethod method, decimal amount, string? reference = null, CancellationToken ct = default)
    {
        // Esta es una operación transaccional: la orden y el primer pago se guardan juntos.
        // Si algo falla (ej. la caja no está abierta para el efectivo), no queda una orden huérfana.
        OrderRules.ValidateForPayment(order, amount);
        if (method == PaymentMethod.Transfer && string.IsNullOrWhiteSpace(reference)) reference = "Sin referencia";
        order.Payments.Add(new Payment { Method = method, Amount = amount, Reference = reference });
        if (order.IsFullyPaid) order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = await store.SaveOrderAsync(order, ct);
        if (saved.Status == OrderStatus.Paid) await printer.PrintAsync(saved, ct);
        return saved;
    }
    public async Task<Order> AddPaymentAsync(Guid orderId, PaymentMethod method, decimal amount, string? reference = null, CancellationToken ct = default)
    {
        var order = await store.GetOrderAsync(orderId, ct) ?? throw new InvalidOperationException("Orden no encontrada.");
        OrderRules.ValidateForPayment(order, amount);
        if (method == PaymentMethod.Transfer && string.IsNullOrWhiteSpace(reference)) reference = "Sin referencia";
        order.Payments.Add(new Payment { OrderId = order.Id, Method = method, Amount = amount, Reference = reference });
        if (order.IsFullyPaid) order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        var saved = await store.SaveOrderAsync(order, ct);
        if (saved.Status == OrderStatus.Paid) await printer.PrintAsync(saved, ct);
        return saved;
    }
}
