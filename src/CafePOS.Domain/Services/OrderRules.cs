using CafePOS.Domain.Entities;
namespace CafePOS.Domain.Services;
public static class OrderRules
{
    public static void ValidatePaymentBreakdown(Order order, IEnumerable<Payment> payments)
    {
        if (order.Status == Enums.OrderStatus.Cancelled) throw new InvalidOperationException("No se puede cobrar una orden cancelada.");
        if (order.Items.Count == 0 || order.Total <= 0) throw new InvalidOperationException("La orden no contiene productos cobrables.");
        var breakdown = payments.ToList();
        if (breakdown.Any(x => x.Amount <= 0))
            throw new ArgumentOutOfRangeException(nameof(payments), "Cada pago debe ser mayor a cero.");
        if (breakdown.Any(x => decimal.Round(x.Amount, 2, MidpointRounding.AwayFromZero) != x.Amount))
            throw new InvalidOperationException("Los pagos solo pueden tener dos decimales.");
        if (breakdown.Sum(x => x.Amount) > order.Total)
            throw new InvalidOperationException("Los pagos exceden el total de la orden.");
        if (breakdown.Any(x => x.Method == Enums.PaymentMethod.Transfer && string.IsNullOrWhiteSpace(x.Reference)))
            throw new InvalidOperationException("Capture la referencia de cada transferencia.");
    }
}
