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
        new() { Name = "Latte + muffin", Category = ProductCategory.Coffee, Price = 99m, StockQuantity = 30 },
        new() { Name = "Capuchino + muffin", Category = ProductCategory.Coffee, Price = 99m, StockQuantity = 30 },
        new() { Name = "Chocolate caliente + muffin", Category = ProductCategory.Coffee, Price = 99m, StockQuantity = 30 },
        new() { Name = "Café latte", Category = ProductCategory.Coffee, Price = 65m, StockQuantity = 30 },
        new() { Name = "Capuchino", Category = ProductCategory.Coffee, Price = 65m, StockQuantity = 30 },
        new() { Name = "Café mocha", Category = ProductCategory.Coffee, Price = 65m, StockQuantity = 30 },
        new() { Name = "Café americano", Category = ProductCategory.Coffee, Price = 55m, StockQuantity = 30 },
        new() { Name = "Café de olla", Category = ProductCategory.Coffee, Price = 55m, StockQuantity = 30 },
        new() { Name = "Tisana caliente", Category = ProductCategory.Coffee, Price = 65m, StockQuantity = 30 },
        new() { Name = "Chocolate caliente", Category = ProductCategory.Coffee, Price = 65m, StockQuantity = 30 },
        new() { Name = "Taro caliente", Category = ProductCategory.Coffee, Price = 65m, StockQuantity = 30 },
        new() { Name = "Matcha caliente", Category = ProductCategory.Coffee, Price = 65m, StockQuantity = 30 },
        new() { Name = "Chai caliente", Category = ProductCategory.Coffee, Price = 65m, StockQuantity = 30 },
        new() { Name = "Bebida caliente de temporada", Category = ProductCategory.Coffee, Price = 70m, StockQuantity = 30 },
        new() { Name = "Café latte a las rocas", Category = ProductCategory.Beverages, Price = 75m, StockQuantity = 30 },
        new() { Name = "Tisana helada", Category = ProductCategory.Beverages, Price = 75m, StockQuantity = 30 },
        new() { Name = "Chocolate helado", Category = ProductCategory.Beverages, Price = 75m, StockQuantity = 30 },
        new() { Name = "Taro helado", Category = ProductCategory.Beverages, Price = 75m, StockQuantity = 30 },
        new() { Name = "Matcha helado", Category = ProductCategory.Beverages, Price = 75m, StockQuantity = 30 },
        new() { Name = "Chai helado", Category = ProductCategory.Beverages, Price = 75m, StockQuantity = 30 },
        new() { Name = "Cold brew", Category = ProductCategory.Beverages, Price = 75m, StockQuantity = 30 },
        new() { Name = "Frapuchino", Category = ProductCategory.Beverages, Price = 78m, StockQuantity = 30 },
        new() { Name = "Botella de agua 500 ml", Category = ProductCategory.Beverages, Price = 16m, StockQuantity = 30 },
        new() { Name = "Bebida helada de temporada", Category = ProductCategory.Beverages, Price = 78m, StockQuantity = 30 },
        new() { Name = "Panini de jamón con queso manchego + bebida", Category = ProductCategory.Food, Price = 149m, StockQuantity = 30 },
        new() { Name = "Panini de pollo con queso manchego + bebida", Category = ProductCategory.Food, Price = 159m, StockQuantity = 30 },
        new() { Name = "Croissant clásico", Category = ProductCategory.Food, Price = 69m, StockQuantity = 30 },
        new() { Name = "Croissant clásico + bebida", Category = ProductCategory.Food, Price = 119m, StockQuantity = 30 },
        new() { Name = "Croissant italiano", Category = ProductCategory.Food, Price = 69m, StockQuantity = 30 },
        new() { Name = "Croissant italiano + bebida", Category = ProductCategory.Food, Price = 119m, StockQuantity = 30 },
        new() { Name = "Croissant saludable", Category = ProductCategory.Food, Price = 69m, StockQuantity = 30 },
        new() { Name = "Croissant saludable + bebida", Category = ProductCategory.Food, Price = 119m, StockQuantity = 30 },
        new() { Name = "Croissant de fresa", Category = ProductCategory.Desserts, Price = 69m, StockQuantity = 30 },
        new() { Name = "Croissant de fresa + bebida", Category = ProductCategory.Desserts, Price = 119m, StockQuantity = 30 },
        new() { Name = "Croissant de manzana y canela", Category = ProductCategory.Desserts, Price = 69m, StockQuantity = 30 },
        new() { Name = "Croissant de manzana y canela + bebida", Category = ProductCategory.Desserts, Price = 119m, StockQuantity = 30 },
        new() { Name = "Croissant cajetoso", Category = ProductCategory.Desserts, Price = 69m, StockQuantity = 30 },
        new() { Name = "Croissant cajetoso + bebida", Category = ProductCategory.Desserts, Price = 119m, StockQuantity = 30 }
    ];

    public static IEnumerable<DeliveryOption> DeliveryOptions() =>
    [
        new() { Name = "Recoge en tienda", Type = DeliveryType.Pickup, Fee = 0m },
        new() { Name = "Entrega caminando", Type = DeliveryType.Walking, Fee = 0m },
        new() { Name = "Entrega local", Type = DeliveryType.Delivery, Fee = 25m }
    ];
}
