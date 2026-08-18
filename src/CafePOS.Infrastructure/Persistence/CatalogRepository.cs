using CafePOS.Application.Interfaces;
using CafePOS.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace CafePOS.Infrastructure.Persistence;

public sealed class CatalogRepository(IDbContextFactory<CafePosDbContext> factory) : ICatalogRepository
{
    public async Task<IReadOnlyList<DeliveryOptionItem>> GetDeliveryOptionsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.DeliveryOptions.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Type).ThenBy(x => x.Fee).ThenBy(x => x.Name)
            .Select(x => new DeliveryOptionItem(x.Id, x.Name, x.Type, x.Fee))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CatalogItem>> GetAvailableItemsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Products.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Category).ThenBy(x => x.Name)
            .Select(x => new CatalogItem(x.Id, x.Name, x.Category, x.Price))
            .ToListAsync(ct);
    }
}
