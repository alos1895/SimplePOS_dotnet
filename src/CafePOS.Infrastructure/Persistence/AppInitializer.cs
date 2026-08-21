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
        await RemoveLegacyProductStockColumnAsync(db, ct);

        if (!await db.Employees.AnyAsync(employee => employee.Id == Seed.DefaultEmployeeId, ct))
            db.Employees.Add(new Employee { Id = Seed.DefaultEmployeeId, DisplayName = "Administrador", Role = EmployeeRole.Admin });
        var catalogInstalled = await db.Products.AnyAsync(
            product => product.Name == Seed.CatalogMarker &&
                       product.Category == ProductCategory.Combos &&
                       product.IsActive, ct);
        if (!catalogInstalled)
        {
            var existingProducts = await db.Products.ToListAsync(ct);
            foreach (var product in existingProducts)
                product.IsActive = false;

            foreach (var seededProduct in Seed.Products())
            {
                var product = existingProducts.FirstOrDefault(existing =>
                    existing.Category == seededProduct.Category && existing.Name == seededProduct.Name);
                if (product is null)
                {
                    product = seededProduct;
                    db.Products.Add(product);
                }
                else
                {
                    product.Price = seededProduct.Price;
                    product.IsActive = true;
                    product.UpdatedAt = DateTime.UtcNow;
                }

            }
        }
        if (!await db.DeliveryOptions.AnyAsync(ct))
            db.DeliveryOptions.AddRange(Seed.DeliveryOptions());
        await db.SaveChangesAsync(ct);
    }

    private static async Task RemoveLegacyProductStockColumnAsync(CafePosDbContext db, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var closeConnection = connection.State != System.Data.ConnectionState.Open;
        if (closeConnection) await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM pragma_table_info('Products') WHERE name = 'StockQuantity' LIMIT 1;";
            if (await command.ExecuteScalarAsync(ct) is null) return;
        }
        finally
        {
            if (closeConnection) await connection.CloseAsync();
        }

        await db.Database.ExecuteSqlRawAsync("""
            PRAGMA foreign_keys = OFF;

            CREATE TABLE Products_CurrentSchema (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Category INTEGER NOT NULL,
                Price INTEGER NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            INSERT INTO Products_CurrentSchema (Id, Name, Category, Price, IsActive, CreatedAt, UpdatedAt)
            SELECT Id, Name, Category, Price, IsActive, CreatedAt, UpdatedAt
            FROM Products;

            DROP TABLE Products;
            ALTER TABLE Products_CurrentSchema RENAME TO Products;
            CREATE UNIQUE INDEX IX_Products_Category_Name ON Products(Category, Name);

            PRAGMA foreign_keys = ON;
            """, ct);
    }

}

public static class Seed
{
    public static readonly Guid DefaultEmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string CatalogMarker = "Panini mixto + bebida";

    public static IEnumerable<Product> Products() =>
    [
        new() { Name = "Latte + muffin", Category = ProductCategory.Combos, Price = 99m },
        new() { Name = "Capuchino + muffin", Category = ProductCategory.Combos, Price = 99m },
        new() { Name = "Chocolate caliente + muffin", Category = ProductCategory.Combos, Price = 99m },
        new() { Name = "Café latte", Category = ProductCategory.Coffee, Price = 65m },
        new() { Name = "Capuchino", Category = ProductCategory.Coffee, Price = 65m },
        new() { Name = "Café mocha", Category = ProductCategory.Coffee, Price = 65m },
        new() { Name = "Café americano", Category = ProductCategory.Coffee, Price = 55m },
        new() { Name = "Café de olla", Category = ProductCategory.Coffee, Price = 55m },
        new() { Name = "Tisana caliente", Category = ProductCategory.Coffee, Price = 65m },
        new() { Name = "Chocolate caliente", Category = ProductCategory.Coffee, Price = 65m },
        new() { Name = "Taro caliente", Category = ProductCategory.Coffee, Price = 65m },
        new() { Name = "Matcha caliente", Category = ProductCategory.Coffee, Price = 65m },
        new() { Name = "Chai caliente", Category = ProductCategory.Coffee, Price = 65m },
        new() { Name = "Bebida caliente de temporada", Category = ProductCategory.Coffee, Price = 70m },
        new() { Name = "Latte tradicional helado", Category = ProductCategory.Beverages, Price = 75m },
        new() { Name = "Latte caramel helado", Category = ProductCategory.Beverages, Price = 75m },
        new() { Name = "Latte moka helado", Category = ProductCategory.Beverages, Price = 75m },
        new() { Name = "Tisana helada", Category = ProductCategory.Beverages, Price = 75m },
        new() { Name = "Chocolate helado", Category = ProductCategory.Beverages, Price = 75m },
        new() { Name = "Taro helado", Category = ProductCategory.Beverages, Price = 75m },
        new() { Name = "Matcha helado", Category = ProductCategory.Beverages, Price = 75m },
        new() { Name = "Chai helado", Category = ProductCategory.Beverages, Price = 75m },
        new() { Name = "Cold brew", Category = ProductCategory.Beverages, Price = 75m },
        new() { Name = "Frapuchino", Category = ProductCategory.Beverages, Price = 78m },
        new() { Name = "Botella de agua 500 ml", Category = ProductCategory.Beverages, Price = 16m },
        new() { Name = "Bebida helada de temporada", Category = ProductCategory.Beverages, Price = 78m },
        new() { Name = "Panini de jamón con queso manchego + bebida", Category = ProductCategory.Combos, Price = 149m },
        new() { Name = "Panini de pollo con queso manchego + bebida", Category = ProductCategory.Combos, Price = 159m },
        new() { Name = "Panini de pollo", Category = ProductCategory.Food, Price = 109m },
        new() { Name = "Panini de jamón", Category = ProductCategory.Food, Price = 99m },
        new() { Name = "Alimento de temporada", Category = ProductCategory.Food, Price = 109m },
        new() { Name = "Ensalada", Category = ProductCategory.Food, Price = 99m },
        new() { Name = "Postre 1", Category = ProductCategory.Desserts, Price = 35m },
        new() { Name = "Postre 2", Category = ProductCategory.Desserts, Price = 40m },
        new() { Name = "Postre 3", Category = ProductCategory.Desserts, Price = 45m },
        new() { Name = "Combo de temporada", Category = ProductCategory.Combos, Price = 149m },
        new() { Name = "Panini mixto + bebida", Category = ProductCategory.Combos, Price = 169m },
        new() { Name = "Proteína", Category = ProductCategory.Extras, Price = 18m },
        new() { Name = "Aderezo", Category = ProductCategory.Extras, Price = 20m },
        new() { Name = "Expresso", Category = ProductCategory.Extras, Price = 18m },
        new() { Name = "Croissant clásico", Category = ProductCategory.Food, Price = 69m },
        new() { Name = "Croissant clásico + bebida", Category = ProductCategory.Combos, Price = 119m },
        new() { Name = "Croissant italiano", Category = ProductCategory.Food, Price = 69m },
        new() { Name = "Croissant italiano + bebida", Category = ProductCategory.Combos, Price = 119m },
        new() { Name = "Croissant saludable", Category = ProductCategory.Food, Price = 69m },
        new() { Name = "Croissant saludable + bebida", Category = ProductCategory.Combos, Price = 119m },
        new() { Name = "Croissant de fresa", Category = ProductCategory.Desserts, Price = 69m },
        new() { Name = "Croissant de fresa + bebida", Category = ProductCategory.Combos, Price = 119m },
        new() { Name = "Croissant de manzana y canela", Category = ProductCategory.Desserts, Price = 69m },
        new() { Name = "Croissant de manzana y canela + bebida", Category = ProductCategory.Combos, Price = 119m },
        new() { Name = "Croissant cajetoso", Category = ProductCategory.Desserts, Price = 69m },
        new() { Name = "Croissant cajetoso + bebida", Category = ProductCategory.Combos, Price = 119m }
    ];

    public static IEnumerable<DeliveryOption> DeliveryOptions() =>
    [
        new() { Name = "Recoge en tienda", Type = DeliveryType.Pickup, Fee = 0m },
        new() { Name = "Entrega caminando", Type = DeliveryType.Walking, Fee = 0m },
        new() { Name = "Entrega local", Type = DeliveryType.Delivery, Fee = 25m }
    ];
}
