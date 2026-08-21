using CafePOS.Application.Interfaces;
using CafePOS.Application.Services;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Domain.Services;
using CafePOS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CafePOS.Tests;

public sealed class OperationalHardeningTests
{
    [Fact]
    public async Task Paid_cancellation_creates_refund_and_zeroes_cash_report()
    {
        var path = NewPath();
        try
        {
            var (options, employee, product) = await SetupAsync(path);
            var orders = new OrderRepository(new TestFactory(options));
            var order = await orders.SaveOrderAsync(Draft(employee, product));
            var checkout = new CheckoutService(orders);
            await checkout.ReplacePaymentsAsync(order.Id, [new Payment { Method = PaymentMethod.Cash, Amount = 100m }], "Cobro inicial");

            var cancelled = await checkout.CancelAsync(order.Id, "Cliente desistió");
            Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
            Assert.Equal(0m, cancelled.PaidAmount);
            Assert.Single(cancelled.Payments, x => x.Kind == PaymentKind.Refund);

            var report = await new CashReportService(
                orders,
                new ManualTransactionRepository(new TestFactory(options))).GetDailyAsync(DateTime.Today);
            Assert.Equal(0m, report.CashOrders);
            Assert.Equal(0m, report.TotalInCaja);
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Split_payment_is_rejected()
    {
        var path = NewPath();
        try
        {
            var (options, employee, product) = await SetupAsync(path);
            var orders = new OrderRepository(new TestFactory(options));
            var order = await orders.SaveOrderAsync(Draft(employee, product));
            var checkout = new CheckoutService(orders);

            await Assert.ThrowsAsync<InvalidOperationException>(() => checkout.ReplacePaymentsAsync(order.Id,
            [
                new Payment { Method = PaymentMethod.Cash, Amount = 40m },
                new Payment { Method = PaymentMethod.Card, Amount = 60m }
            ], "Pago dividido"));
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Active_product_can_be_sold_repeatedly_without_quantity_limits()
    {
        var path = NewPath();
        try
        {
            var (options, employee, product) = await SetupAsync(path);
            var orders = new OrderRepository(new TestFactory(options));

            await orders.SaveOrderAsync(Draft(employee, product));
            await orders.SaveOrderAsync(Draft(employee, product));

            await using var db = new CafePosDbContext(options);
            Assert.Equal(2, await db.Orders.CountAsync());
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Delivery_requires_normalized_client_details_and_defers_cash_on_delivery()
    {
        var path = NewPath();
        try
        {
            var (options, employee, product) = await SetupAsync(path);
            var orders = new OrderRepository(new TestFactory(options));
            var service = new OrderService(orders);
            var invalid = Draft(employee, product);
            invalid.DeliveryType = DeliveryType.Delivery;
            invalid.DeliveryAddress = "Calle 1";
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(invalid));

            var delivery = Draft(employee, product);
            delivery.DeliveryType = DeliveryType.Delivery;
            delivery.CustomerName = "Ana";
            delivery.CustomerPhone = "(55) 1234-5678";
            delivery.DeliveryAddress = "Calle 1";
            delivery.CashOnDelivery = true;
            var saved = await service.CreateAsync(delivery);
            Assert.Equal("+5512345678", saved.CustomerPhone);
            Assert.Equal(DeliveryStatus.Preparing, saved.DeliveryStatus);

            var checkout = new CheckoutService(orders);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                checkout.ReplacePaymentsAsync(saved.Id, [new Payment { Method = PaymentMethod.Cash, Amount = 100m }], "Cobro COD"));
            await checkout.UpdateDeliveryAsync(saved.Id, DeliveryStatus.Delivered, "Rider 1", null, true);
            var paid = await checkout.ReplacePaymentsAsync(saved.Id, [new Payment { Method = PaymentMethod.Cash, Amount = 100m }], "Cobro al entregar");
            Assert.True(paid.IsFullyPaid);
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Business_date_daily_numbers_are_unique_and_metrics_separate_invoiced_collected_and_open()
    {
        var path = NewPath();
        try
        {
            var (options, employee, product) = await SetupAsync(path);
            var orders = new OrderRepository(new TestFactory(options));
            var first = Draft(employee, product);
            first.BusinessDate = BusinessDate.FromLocalDate(DateTime.Today);
            var second = Draft(employee, product);
            second.BusinessDate = first.BusinessDate;
            var savedFirst = await orders.SaveOrderAsync(first);
            var savedSecond = await orders.SaveOrderAsync(second);
            Assert.Equal(1, savedFirst.DailyNumber);
            Assert.Equal(2, savedSecond.DailyNumber);

            var checkout = new CheckoutService(orders);
            await checkout.ReplacePaymentsAsync(savedFirst.Id, [new Payment { Method = PaymentMethod.Card, Amount = 100m }], "Cobro");
            var metrics = await new BusinessMetricsService(orders).GetAsync(DateTime.Today, DateTime.Today);
            Assert.Equal(200m, metrics.InvoicedSales);
            Assert.Equal(100m, metrics.CollectedPayments);
            Assert.Equal(100m, metrics.OutstandingBalance);
            Assert.Equal(100m, metrics.AverageTicket);
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Manual_transaction_reversal_preserves_original_and_adds_opposite_entry()
    {
        var path = NewPath();
        try
        {
            var (options, employee, _) = await SetupAsync(path);
            var repository = new ManualTransactionRepository(new TestFactory(options));
            var original = await repository.AddAsync(new ManualTransaction
            {
                Concept = "Fondo inicial",
                Amount = 50m,
                Type = ManualTransactionType.Income,
                EmployeeId = employee.Id
            });
            var reversal = await repository.ReverseAsync(original.Id, "Captura equivocada", employee.Id);

            Assert.Equal(ManualTransactionKind.Reversal, reversal.Kind);
            Assert.Equal(ManualTransactionType.Expense, reversal.Type);
            await using var db = new CafePosDbContext(options);
            Assert.Equal(2, await db.ManualTransactions.CountAsync());
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Initializer_creates_and_seeds_the_clean_cafe_schema()
    {
        var path = NewPath();
        try
        {
            var options = new DbContextOptionsBuilder<CafePosDbContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options;
            var initializer = new AppInitializer(
                new TestPaths(path),
                new NoBackup(),
                new TestFactory(options),
                NullLogger<AppInitializer>.Instance);

            await initializer.InitializeAsync();

            await using var db = new CafePosDbContext(options);
            Assert.True(await db.Employees.AnyAsync());
            var menu = await db.Products.Where(x => x.IsActive).ToListAsync();
            Assert.Equal(52, menu.Count);
            Assert.Contains(menu, x => x.Name == "Panini de jamón con queso manchego + bebida" && x.Price == 149m);
            Assert.Contains(menu, x => x.Name == "Panini de pollo con queso manchego + bebida" && x.Price == 159m);
            Assert.Contains(menu, x => x.Name == "Latte + muffin" && x.Price == 99m);
            Assert.Equal(6, menu.Count(x => x.Name.EndsWith("+ bebida") && x.Name.StartsWith("Croissant")));
            Assert.Equal(6, menu.Count(x => x.Price == 69m));
            Assert.Contains(menu, x => x.Name == "Café americano" && x.Price == 55m && x.Category == ProductCategory.Coffee);
            Assert.DoesNotContain(menu, x => x.Name == "Café latte a las rocas");
            Assert.Contains(menu, x => x.Name == "Latte tradicional helado" && x.Price == 75m && x.Category == ProductCategory.Beverages);
            Assert.Contains(menu, x => x.Name == "Latte caramel helado" && x.Price == 75m && x.Category == ProductCategory.Beverages);
            Assert.Contains(menu, x => x.Name == "Latte moka helado" && x.Price == 75m && x.Category == ProductCategory.Beverages);
            Assert.Contains(menu, x => x.Name == "Panini de pollo" && x.Price == 109m && x.Category == ProductCategory.Food);
            Assert.Contains(menu, x => x.Name == "Panini de jamón" && x.Price == 99m && x.Category == ProductCategory.Food);
            Assert.Contains(menu, x => x.Name == "Alimento de temporada" && x.Price == 109m && x.Category == ProductCategory.Food);
            Assert.Contains(menu, x => x.Name == "Ensalada" && x.Price == 99m && x.Category == ProductCategory.Food);
            Assert.Contains(menu, x => x.Name == "Postre 1" && x.Price == 35m && x.Category == ProductCategory.Desserts);
            Assert.Contains(menu, x => x.Name == "Postre 2" && x.Price == 40m && x.Category == ProductCategory.Desserts);
            Assert.Contains(menu, x => x.Name == "Postre 3" && x.Price == 45m && x.Category == ProductCategory.Desserts);
            Assert.Contains(menu, x => x.Name == "Combo de temporada" && x.Price == 149m && x.Category == ProductCategory.Combos);
            Assert.Contains(menu, x => x.Name == "Panini mixto + bebida" && x.Price == 169m && x.Category == ProductCategory.Combos);
            Assert.Contains(menu, x => x.Name == "Proteína" && x.Price == 18m && x.Category == ProductCategory.Extras);
            Assert.Contains(menu, x => x.Name == "Aderezo" && x.Price == 20m && x.Category == ProductCategory.Extras);
            Assert.Contains(menu, x => x.Name == "Expresso" && x.Price == 18m && x.Category == ProductCategory.Extras);
            Assert.Contains(menu, x => x.Name == "Frapuchino" && x.Price == 78m);
            Assert.Contains(menu, x => x.Name == "Botella de agua 500 ml" && x.Price == 16m);
            Assert.Equal(13, menu.Count(x => x.Category == ProductCategory.Combos));
            Assert.All(menu.Where(x => x.Name.Contains("+ bebida") || x.Name.Contains("+ muffin")),
                product => Assert.Equal(ProductCategory.Combos, product.Category));
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Initializer_replaces_the_old_six_item_development_catalog()
    {
        var path = NewPath();
        try
        {
            var options = new DbContextOptionsBuilder<CafePosDbContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options;
            await using (var db = new CafePosDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.Products.AddRange(
                    new Product { Name = "Espresso", Category = ProductCategory.Coffee, Price = 35m },
                    new Product { Name = "Latte", Category = ProductCategory.Coffee, Price = 55m },
                    new Product { Name = "Té helado", Category = ProductCategory.Beverages, Price = 35m },
                    new Product { Name = "Sándwich del día", Category = ProductCategory.Food, Price = 85m },
                    new Product { Name = "Panqué", Category = ProductCategory.Desserts, Price = 45m },
                    new Product { Name = "Shot extra", Category = ProductCategory.Extras, Price = 15m });
                await db.SaveChangesAsync();
            }

            var initializer = new AppInitializer(
                new TestPaths(path), new NoBackup(), new TestFactory(options), NullLogger<AppInitializer>.Instance);
            await initializer.InitializeAsync();

            await using var verification = new CafePosDbContext(options);
            Assert.Equal(52, await verification.Products.CountAsync(x => x.IsActive));
            Assert.False((await verification.Products.SingleAsync(x => x.Name == "Espresso")).IsActive);
            Assert.True(await verification.Products.AnyAsync(x =>
                x.Name == Seed.CatalogMarker && x.Category == ProductCategory.Combos && x.IsActive));
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Initializer_moves_existing_food_and_drink_products_into_combos()
    {
        var path = NewPath();
        try
        {
            var options = new DbContextOptionsBuilder<CafePosDbContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options;
            await using (var db = new CafePosDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.Products.Add(new Product
                {
                    Name = Seed.CatalogMarker,
                    Category = ProductCategory.Food,
                    Price = 149m
                });
                await db.SaveChangesAsync();
            }

            var initializer = new AppInitializer(
                new TestPaths(path), new NoBackup(), new TestFactory(options), NullLogger<AppInitializer>.Instance);
            await initializer.InitializeAsync();

            await using var verification = new CafePosDbContext(options);
            Assert.False((await verification.Products.SingleAsync(x =>
                x.Name == Seed.CatalogMarker && x.Category == ProductCategory.Food)).IsActive);
            Assert.True(await verification.Products.AnyAsync(x =>
                x.Name == Seed.CatalogMarker && x.Category == ProductCategory.Combos && x.IsActive));
            Assert.Equal(13, await verification.Products.CountAsync(x =>
                x.Category == ProductCategory.Combos && x.IsActive));
        }
        finally { Delete(path); }
    }

    private static async Task<(DbContextOptions<CafePosDbContext> Options, Employee Employee, Product Product)> SetupAsync(string path)
    {
        var options = new DbContextOptionsBuilder<CafePosDbContext>()
            .UseSqlite($"Data Source={path};Pooling=False").Options;
        var employee = new Employee { DisplayName = "Tester", Role = EmployeeRole.Admin };
        var product = new Product { Name = "Latte", Category = ProductCategory.Coffee, Price = 100m };
        await using var db = new CafePosDbContext(options);
        await db.Database.MigrateAsync();
        db.AddRange(employee, product);
        await db.SaveChangesAsync();
        return (options, employee, product);
    }

    private static Order Draft(Employee employee, Product product) => new()
    {
        EmployeeId = employee.Id,
        Items = [new OrderItem { ProductId = product.Id, ProductName = product.Name, Category = product.Category, UnitPrice = product.Price, Quantity = 1 }]
    };

    private static string NewPath() => Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.db");

    private static void Delete(string path)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(path)) File.Delete(path);
    }

    private sealed class TestFactory(DbContextOptions<CafePosDbContext> options) : IDbContextFactory<CafePosDbContext>
    {
        public CafePosDbContext CreateDbContext() => new(options);
        public Task<CafePosDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new CafePosDbContext(options));
    }

    private sealed class TestPaths(string databasePath) : IDataPathProvider
    {
        public string DataDirectory => Path.GetDirectoryName(databasePath)!;
        public string DatabasePath => databasePath;
        public string BackupDirectory => Path.Combine(DataDirectory, "backups");
        public string LogDirectory => Path.Combine(DataDirectory, "logs");
        public string SettingsPath => Path.Combine(DataDirectory, "settings.json");
    }

    private sealed class NoBackup : IBackupService
    {
        public Task<string?> CreateAsync(string reason, CancellationToken ct = default) => Task.FromResult<string?>(null);
        public Task RotateAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
