using CafePOS.Application.Interfaces;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Domain.Services;

namespace CafePOS.Application.Services;

public sealed class OrderService(IOrderRepository orders, ICurrentUserContext? user = null)
{
    public Task<Order> CreateAsync(Order order, CancellationToken ct = default)
    {
        if (order.Items.Count == 0) throw new InvalidOperationException("Agregue al menos un producto.");
        if (order.Total <= 0) throw new InvalidOperationException("La orden debe tener un total mayor a cero.");
        if (order.Items.Any(x => x.ProductId is null || x.Quantity <= 0))
            throw new InvalidOperationException("Cada partida debe tener un producto y una cantidad válida.");
        if (user is not null) order.EmployeeId = user.Current.EmployeeId;
        if (order.EmployeeId == Guid.Empty) throw new InvalidOperationException("No hay un usuario activo para registrar la orden.");
        if (order.DeliveryType is DeliveryType.Delivery or DeliveryType.Walking)
        {
            if (string.IsNullOrWhiteSpace(order.CustomerName))
                throw new InvalidOperationException("Las entregas requieren el nombre del cliente.");
            if (string.IsNullOrWhiteSpace(order.DeliveryAddress))
                throw new InvalidOperationException("Las entregas requieren una dirección.");
            order.CustomerPhone = CustomerDetails.NormalizePhone(order.CustomerPhone);
            if (order.DeliveryStatus == DeliveryStatus.None) order.DeliveryStatus = DeliveryStatus.Preparing;
        }
        else
        {
            order.DeliveryStatus = DeliveryStatus.None;
            order.CashOnDelivery = false;
        }
        return orders.SaveOrderAsync(order, ct);
    }
}
