using CafePOS.Application.Interfaces;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Services;

namespace CafePOS.Application.Services;

public sealed class CheckoutService(IOrderRepository orders, ICurrentUserContext? user = null)
{
    public async Task<Order> ReplacePaymentsAsync(
        Guid orderId,
        IReadOnlyList<Payment> payments,
        string reason,
        CancellationToken ct = default)
    {
        var order = await orders.GetOrderAsync(orderId, ct) ?? throw new InvalidOperationException("Orden no encontrada.");
        if (order.Status != Domain.Enums.OrderStatus.Open)
            throw new InvalidOperationException("Solo se puede modificar el desglose de una orden abierta.");
        if (order.CashOnDelivery && order.DeliveryStatus != Domain.Enums.DeliveryStatus.Delivered)
            throw new InvalidOperationException("El efectivo contra entrega solo se registra cuando la entrega fue marcada como entregada.");
        if (order.CurrentCollections.Count > 0 && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Escriba el motivo del ajuste de pagos.");

        var normalized = payments.Select(x => new Payment
        {
            Id = Guid.NewGuid(),
            Method = x.Method,
            Amount = x.Amount,
            Reference = string.IsNullOrWhiteSpace(x.Reference) ? null : x.Reference.Trim(),
            CreatedAt = DateTime.UtcNow,
            EmployeeId = user?.Current.EmployeeId ?? order.EmployeeId
        }).ToList();
        OrderRules.ValidatePaymentBreakdown(order, normalized);
        return await orders.ReplacePaymentsAsync(
            orderId,
            normalized,
            string.IsNullOrWhiteSpace(reason) ? "Registro inicial de pago" : reason.Trim(),
            user?.Current.EmployeeId ?? order.EmployeeId,
            ct);
    }

    public Task<Order> CancelAsync(Guid orderId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Escriba el motivo de cancelación.");
        return orders.CancelOrderAsync(orderId, reason.Trim(), user?.Current.EmployeeId ?? Guid.Empty, ct);
    }

    public async Task<Order> UpdateDeliveryAsync(
        Guid orderId,
        Domain.Enums.DeliveryStatus status,
        string riderName,
        DateTime? promisedAt,
        bool cashOnDelivery,
        CancellationToken ct = default)
    {
        var order = await orders.GetOrderAsync(orderId, ct) ?? throw new InvalidOperationException("Orden no encontrada.");
        if (order.DeliveryType == Domain.Enums.DeliveryType.Pickup)
            throw new InvalidOperationException("Esta orden no requiere seguimiento de entrega.");
        if (status is Domain.Enums.DeliveryStatus.OutForDelivery or Domain.Enums.DeliveryStatus.Delivered &&
            string.IsNullOrWhiteSpace(riderName))
            throw new InvalidOperationException("Indique el repartidor antes de enviar o entregar la orden.");
        return await orders.UpdateDeliveryAsync(
            orderId, status, riderName.Trim(), promisedAt, cashOnDelivery,
            user?.Current.EmployeeId ?? order.EmployeeId, ct);
    }
}
