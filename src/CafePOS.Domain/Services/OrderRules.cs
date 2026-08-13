using CafePOS.Domain.Entities;
namespace CafePOS.Domain.Services;
public static class OrderRules
{
    public static void ValidateForPayment(Order order, decimal amount)
    {
        if (order.Status == Enums.OrderStatus.Cancelled) throw new InvalidOperationException("No se puede cobrar una orden cancelada.");
        if (order.Items.Count == 0 || order.Total <= 0) throw new InvalidOperationException("La orden no contiene productos cobrables.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "El pago debe ser mayor a cero.");
        if (amount > order.BalanceDue) throw new InvalidOperationException("El pago excede el saldo pendiente.");
    }
}
