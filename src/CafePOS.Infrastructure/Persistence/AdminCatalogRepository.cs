using CafePOS.Application.Interfaces;
using CafePOS.Application.Models;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafePOS.Infrastructure.Persistence;

public sealed class AdminCatalogRepository(IDbContextFactory<CafePosDbContext> factory) : IAdminCatalogRepository
{
    public async Task<AdminCatalogData> GetAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var products = await db.Products.AsNoTracking()
            .OrderBy(x => x.Category).ThenBy(x => x.Name)
            .Select(x => new AdminProductItem(x.Id, x.Name, x.Category, x.Price, x.IsActive))
            .ToListAsync(ct);
        var deliveryOptions = await db.DeliveryOptions.AsNoTracking()
            .OrderBy(x => x.Type).ThenBy(x => x.Name)
            .Select(x => new AdminDeliveryOptionItem(x.Id, x.Name, x.Type, x.Fee, x.IsActive))
            .ToListAsync(ct);
        return new AdminCatalogData(products, deliveryOptions);
    }

    public async Task SaveProductAsync(ProductUpsert input, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var isNew = input.Id is null;
        var product = isNew
            ? new Product { Name = input.Name }
            : await db.Products.SingleOrDefaultAsync(x => x.Id == input.Id, ct)
                ?? throw new InvalidOperationException("Producto no encontrado.");

        product.Name = input.Name;
        product.Category = input.Category;
        product.Price = input.Price;
        product.IsActive = true;
        product.UpdatedAt = DateTime.UtcNow;
        if (isNew) db.Products.Add(product);

        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateProductAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Producto no encontrado.");
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveDeliveryOptionAsync(DeliveryOptionUpsert input, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var option = input.Id is null
            ? new DeliveryOption { Name = input.Name }
            : await db.DeliveryOptions.SingleOrDefaultAsync(x => x.Id == input.Id, ct)
                ?? throw new InvalidOperationException("Opción de entrega no encontrada.");
        option.Name = input.Name;
        option.Type = input.Type;
        option.Fee = input.Fee;
        option.IsActive = true;
        if (input.Id is null) db.DeliveryOptions.Add(option);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateDeliveryOptionAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var option = await db.DeliveryOptions.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Opción de entrega no encontrada.");
        option.IsActive = false;
        await db.SaveChangesAsync(ct);
    }
}
