using CafePOS.Application.Services;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CafePOS.Tests;

public sealed class CafeSchemaAndInventoryTests
{
    [Fact]
    public async Task Migration_creates_cafe_tables_without_specialized_catalog_tables()
    {
        var databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.db");
        try
        {
            var options = Options(databasePath);
            await using var db = new CafePosDbContext(options);
            await db.Database.MigrateAsync();
            await db.Database.OpenConnectionAsync();
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name IN ('Products', 'InventoryMovements', 'DeliveryOptions', 'Orders', 'OrderItems', 'Payments', 'ManualTransactions');
                """;
            Assert.Equal(7, Convert.ToInt32(await command.ExecuteScalarAsync()));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Saving_and_cancelling_order_decrements_and_restores_product_stock()
    {
        var databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.db");
        try
        {
            var options = Options(databasePath);
            var employee = new Employee { DisplayName = "Cajero" };
            var product = new Product { Name = "Latte", Category = ProductCategory.Coffee, Price = 55m, StockQuantity = 2 };
            await using (var db = new CafePosDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.AddRange(employee, product);
                await db.SaveChangesAsync();
            }

            var repository = new OrderRepository(new TestDbContextFactory(options));
            var order = new Order
            {
                EmployeeId = employee.Id,
                Items = [new OrderItem { ProductId = product.Id, ProductName = "stale name", Category = ProductCategory.Extras, UnitPrice = 1m, Quantity = 1 }]
            };

            await repository.SaveOrderAsync(order);
            await using (var db = new CafePosDbContext(options))
            {
                Assert.Equal(1, (await db.Products.SingleAsync()).StockQuantity);
                Assert.Equal(-1, (await db.InventoryMovements.SingleAsync()).QuantityDelta);
                Assert.Equal("Latte", (await db.OrderItems.SingleAsync()).ProductName);
            }

            await repository.CancelOrderAsync(order.Id, "Cliente canceló", employee.Id);
            await using (var db = new CafePosDbContext(options))
            {
                Assert.Equal(2, (await db.Products.SingleAsync()).StockQuantity);
                Assert.Equal(2, await db.InventoryMovements.CountAsync());
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Replacing_an_open_order_payment_breakdown_updates_balance_and_status()
    {
        var databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.db");
        try
        {
            var options = Options(databasePath);
            var employee = new Employee { DisplayName = "Cajero" };
            var product = new Product { Name = "Americano", Category = ProductCategory.Coffee, Price = 100m, StockQuantity = 2 };
            await using (var db = new CafePosDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.AddRange(employee, product);
                await db.SaveChangesAsync();
            }

            var repository = new OrderRepository(new TestDbContextFactory(options));
            var order = await repository.SaveOrderAsync(new Order
            {
                EmployeeId = employee.Id,
                Items = [new OrderItem { ProductId = product.Id, ProductName = product.Name, Category = product.Category, UnitPrice = product.Price, Quantity = 1 }]
            });
            var checkout = new CheckoutService(repository);
            var partial = await checkout.ReplacePaymentsAsync(order.Id,
                [new Payment { Method = PaymentMethod.Cash, Amount = 40m }], "Cobro inicial");

            Assert.Equal(OrderStatus.Open, partial.Status);
            Assert.Equal(60m, partial.BalanceDue);

            var paid = await checkout.ReplacePaymentsAsync(order.Id,
            [
                new Payment { Id = partial.Payments.Single().Id, Method = PaymentMethod.Cash, Amount = 40m },
                new Payment { Method = PaymentMethod.Transfer, Amount = 60m, Reference = "SPEI-123" }
            ], "Completar con transferencia");

            Assert.Equal(OrderStatus.Paid, paid.Status);
            Assert.Equal(0m, paid.BalanceDue);
            Assert.Equal(2, paid.CurrentCollections.Count);
            Assert.Equal(4, paid.Payments.Count);
            Assert.Single(paid.Payments, x => x.Kind == PaymentKind.Reversal);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private static DbContextOptions<CafePosDbContext> Options(string databasePath) =>
        new DbContextOptionsBuilder<CafePosDbContext>().UseSqlite($"Data Source={databasePath};Pooling=False").Options;

    private sealed class TestDbContextFactory(DbContextOptions<CafePosDbContext> options) : IDbContextFactory<CafePosDbContext>
    {
        public CafePosDbContext CreateDbContext() => new(options);
        public Task<CafePosDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new CafePosDbContext(options));
    }
}
