using CafePOS.Application.Interfaces;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Domain.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CafePOS.Infrastructure.Persistence;

public sealed class OrderRepository(IDbContextFactory<CafePosDbContext> factory) : IOrderRepository
{
    public async Task<IReadOnlyList<Order>> GetOrdersAsync(
        DateTime from,
        DateTime to,
        string? search,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var start = BusinessDate.FromLocalDate(from);
        var end = BusinessDate.FromLocalDate(to);
        IQueryable<Order> query = db.Orders.AsNoTracking()
            .Where(x => string.Compare(x.BusinessDate, start) >= 0 && string.Compare(x.BusinessDate, end) < 0)
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .Include(x => x.Employee);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var hasDailyNumber = int.TryParse(term, out var dailyNumber);
            query = query.Where(x =>
                x.CustomerName.Contains(term) ||
                x.CustomerPhone.Contains(term) ||
                x.Comments.Contains(term) ||
                (x.CancellationReason != null && x.CancellationReason.Contains(term)) ||
                x.RiderName.Contains(term) ||
                x.Items.Any(i => i.ProductName.Contains(term)) ||
                x.Payments.Any(p => p.Reference != null && p.Reference.Contains(term)) ||
                (hasDailyNumber && x.DailyNumber == dailyNumber));
        }

        return await query.OrderByDescending(x => x.BusinessDate).ThenByDescending(x => x.DailyNumber).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var start = BusinessDate.FromLocalDate(from);
        var end = BusinessDate.FromLocalDate(to);
        return await db.Payments.AsNoTracking()
            .Where(x => string.Compare(x.BusinessDate, start) >= 0 && string.Compare(x.BusinessDate, end) < 0)
            .OrderBy(x => x.BusinessDate).ThenBy(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task<Order?> GetOrderAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Orders.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .Include(x => x.Employee)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Order> SaveOrderAsync(Order order, CancellationToken ct = default)
    {
        ValidateDeliveryRequirements(order);
        await using var probe = await factory.CreateDbContextAsync(ct);
        if (!await probe.Orders.AnyAsync(x => x.Id == order.Id, ct))
            return await SaveNewOrderWithRetryAsync(order, ct);

        var stored = await probe.Orders.Include(x => x.Items).Include(x => x.Payments)
            .SingleAsync(x => x.Id == order.Id, ct);
        if (stored.Status != OrderStatus.Open)
            throw new InvalidOperationException("Solo se pueden editar órdenes abiertas.");
        stored.CustomerName = order.CustomerName.Trim();
        stored.CustomerPhone = order.CustomerPhone.Trim();
        stored.Comments = order.Comments.Trim();
        stored.DeliveryAddress = order.DeliveryAddress.Trim();
        stored.PromisedAt = order.PromisedAt;
        stored.RiderName = order.RiderName.Trim();
        stored.CashOnDelivery = order.CashOnDelivery;
        stored.UpdatedAt = DateTime.UtcNow;
        await probe.SaveChangesAsync(ct);
        return stored;
    }

    public async Task<Order> ReplacePaymentsAsync(
        Guid orderId,
        IReadOnlyList<Payment> payments,
        string reason,
        Guid employeeId,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var order = await db.Orders.Include(x => x.Items).Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == orderId, ct) ?? throw new InvalidOperationException("Orden no encontrada.");
        if (order.Status != OrderStatus.Open)
            throw new InvalidOperationException("Solo se puede pagar una orden abierta.");
        if (order.CashOnDelivery && order.DeliveryStatus != DeliveryStatus.Delivered)
            throw new InvalidOperationException("El pago contra entrega no puede cobrarse antes de la entrega.");
        if (order.CurrentCollections.Count > 0 && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Escriba el motivo del ajuste de pagos.");

        OrderRules.ValidateFullPayment(order, payments);
        var actor = employeeId == Guid.Empty ? order.EmployeeId : employeeId;
        var now = DateTime.UtcNow;
        var businessDate = BusinessDate.FromUtc(now);
        foreach (var existing in order.CurrentCollections)
        {
            db.Payments.Add(new Payment
            {
                OrderId = order.Id,
                EmployeeId = actor,
                BusinessDate = businessDate,
                Method = existing.Method,
                Kind = PaymentKind.Reversal,
                Amount = existing.Amount,
                Reference = existing.Reference,
                ReversesPaymentId = existing.Id,
                Reason = reason,
                CreatedAt = now
            });
        }

        foreach (var payment in payments)
        {
            db.Payments.Add(new Payment
            {
                OrderId = order.Id,
                EmployeeId = actor,
                BusinessDate = businessDate,
                Method = payment.Method,
                Kind = PaymentKind.Collection,
                Amount = payment.Amount,
                Reference = payment.Reference,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Registro de pago" : reason,
                CreatedAt = now
            });
        }

        order.Status = payments.Sum(x => x.Amount) == order.Total ? OrderStatus.Paid : OrderStatus.Open;
        order.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetOrderAsync(order.Id, ct) ?? order;
    }

    public async Task<Order> CancelOrderAsync(Guid orderId, string reason, Guid employeeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Escriba el motivo de cancelación.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var order = await db.Orders.Include(x => x.Items).Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == orderId, ct) ?? throw new InvalidOperationException("Orden no encontrada.");
        if (order.Status == OrderStatus.Cancelled) throw new InvalidOperationException("La orden ya está cancelada.");

        var now = DateTime.UtcNow;
        var actor = employeeId == Guid.Empty ? order.EmployeeId : employeeId;
        foreach (var payment in order.CurrentCollections)
        {
            db.Payments.Add(new Payment
            {
                OrderId = order.Id,
                EmployeeId = actor,
                BusinessDate = BusinessDate.FromUtc(now),
                Method = payment.Method,
                Kind = PaymentKind.Refund,
                Amount = payment.Amount,
                Reference = payment.Reference,
                ReversesPaymentId = payment.Id,
                Reason = $"Reembolso por cancelación: {reason.Trim()}",
                CreatedAt = now
            });
        }

        foreach (var group in order.Items.Where(x => x.ProductId is not null).GroupBy(x => x.ProductId!.Value))
        {
            var product = await db.Products.SingleAsync(x => x.Id == group.Key, ct);
            var quantity = group.Sum(x => x.Quantity);
            product.StockQuantity += quantity;
            product.UpdatedAt = now;
            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = product.Id,
                OrderId = order.Id,
                EmployeeId = actor,
                BusinessDate = BusinessDate.FromUtc(now),
                Type = InventoryMovementType.Cancellation,
                QuantityDelta = quantity,
                Reason = $"Cancelación de orden #{order.DailyNumber}: {reason.Trim()}",
                OccurredAt = now
            });
        }

        order.Status = OrderStatus.Cancelled;
        order.DeliveryStatus = DeliveryStatus.Cancelled;
        order.CancelledAt = now;
        order.CancellationReason = reason.Trim();
        order.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetOrderAsync(order.Id, ct) ?? order;
    }

    public async Task<Order> UpdateDeliveryAsync(
        Guid orderId,
        DeliveryStatus status,
        string riderName,
        DateTime? promisedAt,
        bool cashOnDelivery,
        Guid employeeId,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var order = await db.Orders.Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == orderId, ct) ?? throw new InvalidOperationException("Orden no encontrada.");
        if (order.Status == OrderStatus.Cancelled) throw new InvalidOperationException("Una orden cancelada no puede actualizarse.");
        if (order.DeliveryType == DeliveryType.Pickup) throw new InvalidOperationException("La orden no es de entrega.");
        if (status is DeliveryStatus.OutForDelivery or DeliveryStatus.Delivered && string.IsNullOrWhiteSpace(riderName))
            throw new InvalidOperationException("Indique el repartidor.");
        if (cashOnDelivery && status != DeliveryStatus.Delivered && order.CurrentCollections.Count > 0)
            throw new InvalidOperationException("No puede marcar pago contra entrega después de registrar cobros.");

        order.DeliveryStatus = status;
        order.RiderName = riderName;
        order.PromisedAt = promisedAt;
        order.CashOnDelivery = cashOnDelivery;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return order;
    }

    private async Task<Order> SaveNewOrderWithRetryAsync(Order order, CancellationToken ct)
    {
        const int attempts = 4;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                await PrepareNewOrderAsync(db, order, ct);
                db.Orders.Add(order);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return order;
            }
            catch (Exception ex) when (attempt < attempts && IsConcurrencyFailure(ex))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), ct);
            }
        }

        throw new InvalidOperationException("No se pudo asignar el número diario de la orden tras varios intentos.");
    }

    private static async Task PrepareNewOrderAsync(CafePosDbContext db, Order order, CancellationToken ct)
    {
        if (order.Items.Count == 0 || order.Items.Any(x => x.ProductId is null || x.Quantity <= 0))
            throw new InvalidOperationException("La orden debe incluir productos con cantidades válidas.");
        var now = DateTime.UtcNow;
        order.CreatedAt = order.CreatedAt == default ? now : order.CreatedAt.ToUniversalTime();
        order.BusinessDate = string.IsNullOrWhiteSpace(order.BusinessDate) ? BusinessDate.FromUtc(order.CreatedAt) : order.BusinessDate;
        order.UpdatedAt = now;
        var maxDailyNumber = await db.Orders
            .Where(x => x.BusinessDate == order.BusinessDate)
            .MaxAsync(x => (int?)x.DailyNumber, ct);
        order.DailyNumber = (maxDailyNumber ?? 0) + 1;

        var requests = order.Items.GroupBy(x => x.ProductId!.Value)
            .Select(x => new { ProductId = x.Key, Quantity = x.Sum(i => i.Quantity) })
            .ToList();
        foreach (var request in requests)
        {
            var product = await db.Products.SingleOrDefaultAsync(x => x.Id == request.ProductId, ct)
                ?? throw new InvalidOperationException("Uno de los productos ya no existe.");
            if (!product.IsActive) throw new InvalidOperationException($"{product.Name} ya no está disponible.");
            if (product.StockQuantity < request.Quantity)
                throw new InvalidOperationException($"Inventario insuficiente para {product.Name}.");

            product.StockQuantity -= request.Quantity;
            product.UpdatedAt = now;
            foreach (var item in order.Items.Where(x => x.ProductId == product.Id))
            {
                item.ProductName = product.Name;
                item.Category = product.Category;
                item.UnitPrice = product.Price;
                item.Product = null;
            }
            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = product.Id,
                OrderId = order.Id,
                EmployeeId = order.EmployeeId,
                BusinessDate = order.BusinessDate,
                Type = InventoryMovementType.Sale,
                QuantityDelta = -request.Quantity,
                Reason = $"Venta orden #{order.DailyNumber}",
                OccurredAt = now
            });
        }
    }

    private static bool IsConcurrencyFailure(Exception ex) =>
        ex is DbUpdateException ||
        ex is SqliteException { SqliteErrorCode: 5 or 6 or 19 };

    private static void ValidateDeliveryRequirements(Order order)
    {
        if (order.DeliveryType is not (DeliveryType.Delivery or DeliveryType.Walking)) return;
        if (string.IsNullOrWhiteSpace(order.CustomerName) || string.IsNullOrWhiteSpace(order.DeliveryAddress))
            throw new InvalidOperationException("Las entregas requieren nombre del cliente y dirección.");
        order.CustomerPhone = CustomerDetails.NormalizePhone(order.CustomerPhone);
        if (order.DeliveryStatus == DeliveryStatus.None) order.DeliveryStatus = DeliveryStatus.Preparing;
    }
}
