using CafePOS.Application.Interfaces;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CafePOS.Infrastructure.Persistence;

public sealed class AppInitializer(
    IDataPathProvider paths,
    IBackupService backups,
    IDbContextFactory<CafePosDbContext> factory,
    ILogger<AppInitializer> log) : IAppInitializer
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(paths.DataDirectory);
        Directory.CreateDirectory(paths.LogDirectory);
        await using var db = await factory.CreateDbContextAsync(ct);
        if ((await db.Database.GetPendingMigrationsAsync(ct)).Any())
        {
            log.LogInformation("Applying initial cafe schema");
            await backups.CreateAsync("premigration", ct);
            await db.Database.MigrateAsync(ct);
        }

        if (!await db.Employees.AnyAsync(ct))
            db.Employees.Add(new Employee { Id = Seed.DefaultEmployeeId, DisplayName = "Administrador", Role = EmployeeRole.Admin });
        if (!await db.Products.AnyAsync(ct))
        {
            var products = Seed.Products().ToArray();
            db.Products.AddRange(products);
            db.InventoryMovements.AddRange(products.Select(product => new InventoryMovement
            {
                Product = product,
                EmployeeId = Seed.DefaultEmployeeId,
                Type = InventoryMovementType.Incoming,
                QuantityDelta = product.StockQuantity,
                Reason = "Inventario inicial"
            }));
        }
        if (!await db.DeliveryOptions.AnyAsync(ct))
            db.DeliveryOptions.AddRange(Seed.DeliveryOptions());
        await db.SaveChangesAsync(ct);
    }

}

public static class Seed
{
    public static readonly Guid DefaultEmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static IEnumerable<Product> Products() =>
    [
        new() { Name = "Espresso", Category = ProductCategory.Coffee, Price = 35m, StockQuantity = 30 },
        new() { Name = "Latte", Category = ProductCategory.Coffee, Price = 55m, StockQuantity = 30 },
        new() { Name = "Té helado", Category = ProductCategory.Beverages, Price = 35m, StockQuantity = 24 },
        new() { Name = "Sándwich del día", Category = ProductCategory.Food, Price = 85m, StockQuantity = 12 },
        new() { Name = "Panqué", Category = ProductCategory.Desserts, Price = 45m, StockQuantity = 16 },
        new() { Name = "Shot extra", Category = ProductCategory.Extras, Price = 15m, StockQuantity = 40 }
    ];

    public static IEnumerable<DeliveryOption> DeliveryOptions() =>
    [
        new() { Name = "Recoge en tienda", Type = DeliveryType.Pickup, Fee = 0m },
        new() { Name = "Entrega caminando", Type = DeliveryType.Walking, Fee = 0m },
        new() { Name = "Entrega local", Type = DeliveryType.Delivery, Fee = 25m }
    ];
}
