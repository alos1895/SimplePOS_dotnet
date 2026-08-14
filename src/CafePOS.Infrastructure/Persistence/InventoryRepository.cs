using CafePOS.Application.Interfaces;
using CafePOS.Application.Models;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CafePOS.Infrastructure.Persistence;

public sealed class InventoryRepository(IDbContextFactory<CafePosDbContext> factory) : IInventoryRepository
{
    public async Task<InventorySnapshot> GetSnapshotAsync(DateTime day, CancellationToken ct = default)
    {
        var products = await GetProductStocksAsync(ct);
        var movements = await GetMovementsAsync(day.Date, day.Date.AddDays(1), ct);
        return new InventorySnapshot(products, movements);
    }

    public async Task<IReadOnlyList<ProductStockItem>> GetProductStocksAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Products.AsNoTracking()
            .OrderBy(x => x.Category).ThenBy(x => x.Name)
            .Select(x => new ProductStockItem(x.Id, x.Name, x.Category, x.StockQuantity, x.LowStockThreshold, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InventoryMovementItem>> GetMovementsAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var start = BusinessDate.FromLocalDate(from);
        var end = BusinessDate.FromLocalDate(to);
        return await db.InventoryMovements.AsNoTracking()
            .Where(x => string.Compare(x.BusinessDate, start) >= 0 && string.Compare(x.BusinessDate, end) < 0)
            .OrderByDescending(x => x.BusinessDate).ThenByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Select(x => new InventoryMovementItem(
                x.Id,
                x.ProductId,
                x.Product!.Name,
                x.Type,
                x.QuantityDelta,
                x.Reason,
                x.Supplier,
                x.Reference,
                x.BusinessDate,
                x.OccurredAt))
            .ToListAsync(ct);
    }

    public async Task AdjustAsync(StockAdjustment adjustment, CancellationToken ct = default)
    {
        if (adjustment.Type is InventoryMovementType.Sale or InventoryMovementType.Cancellation)
            throw new InvalidOperationException("Tipo de movimiento no permitido.");
        if (string.IsNullOrWhiteSpace(adjustment.Reason))
            throw new InvalidOperationException("El movimiento requiere un motivo.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == adjustment.ProductId, ct)
            ?? throw new InvalidOperationException("Producto no encontrado.");

        var delta = adjustment.Type switch
        {
            InventoryMovementType.Count => adjustment.Quantity - product.StockQuantity,
            InventoryMovementType.Waste => -Math.Abs(adjustment.Quantity),
            _ => adjustment.Quantity
        };
        if (product.StockQuantity + delta < 0)
            throw new InvalidOperationException($"Inventario insuficiente para {product.Name}.");

        var now = DateTime.UtcNow;
        product.StockQuantity += delta;
        product.UpdatedAt = now;
        db.InventoryMovements.Add(new InventoryMovement
        {
            ProductId = product.Id,
            EmployeeId = adjustment.EmployeeId,
            BusinessDate = BusinessDate.FromLocalDate(adjustment.EffectiveDate),
            Type = adjustment.Type,
            QuantityDelta = delta,
            Reason = adjustment.Reason,
            Supplier = adjustment.Supplier,
            Reference = adjustment.Reference,
            OccurredAt = now
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
