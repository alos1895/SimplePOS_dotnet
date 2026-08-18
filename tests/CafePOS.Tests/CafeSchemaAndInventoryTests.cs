using CafePOS.Application.Services;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CafePOS.Tests;

public sealed class CafeSchemaTests
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
                  AND name IN ('Products', 'DeliveryOptions', 'Orders', 'OrderItems', 'Payments', 'ManualTransactions');
                """;
            Assert.Equal(6, Convert.ToInt32(await command.ExecuteScalarAsync()));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Paying_total_marks_order_as_paid()
    {
        var databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.db");
        try
        {
            var options = Options(databasePath);
            var employee = new Employee { DisplayName = "Cajero" };
            var product = new Product { Name = "Americano", Category = ProductCategory.Coffee, Price = 100m };
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
            var paid = await checkout.PayTotalAsync(order.Id, PaymentMethod.Card);

            Assert.Equal(OrderStatus.Paid, paid.Status);
            Assert.Equal(0m, paid.BalanceDue);
            var payment = Assert.Single(paid.CurrentCollections);
            Assert.Equal(PaymentMethod.Card, payment.Method);
            Assert.Equal(100m, payment.Amount);
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
