using CafePOS.Domain.Entities;
namespace CafePOS.Domain.Services;
public static class OrderRules
{
    public static void ValidateFullPayment(Order order, IEnumerable<Payment> payments)
    {
        if (order.Status == Enums.OrderStatus.Cancelled) throw new InvalidOperationException("No se puede cobrar una orden cancelada.");
        if (order.Items.Count == 0 || order.Total <= 0) throw new InvalidOperationException("La orden no contiene productos cobrables.");
        var entries = payments.ToList();
        if (entries.Count == 0)
            throw new InvalidOperationException("Registre un solo pago por el total de la orden.");
        if (entries.Count > 1)
            throw new InvalidOperationException("No se permiten pagos divididos.");
        var payment = entries[0];
        if (payment.Amount != order.Total)
            throw new InvalidOperationException("El pago debe cubrir el total exacto de la orden.");
        if (decimal.Round(payment.Amount, 2, MidpointRounding.AwayFromZero) != payment.Amount)
            throw new InvalidOperationException("El pago solo puede tener dos decimales.");
    }
}
