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
            var (options, employee, product) = await SetupAsync(path, 3);
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
    public async Task Split_payment_edit_appends_reversal_audit_instead_of_deleting()
    {
        var path = NewPath();
        try
        {
            var (options, employee, product) = await SetupAsync(path, 3);
            var orders = new OrderRepository(new TestFactory(options));
            var order = await orders.SaveOrderAsync(Draft(employee, product));
            var checkout = new CheckoutService(orders);
            await checkout.ReplacePaymentsAsync(order.Id, [new Payment { Method = PaymentMethod.Cash, Amount = 40m }], "Anticipo");
            var paid = await checkout.ReplacePaymentsAsync(order.Id,
            [
                new Payment { Method = PaymentMethod.Cash, Amount = 40m },
                new Payment { Method = PaymentMethod.Card, Amount = 60m }
            ], "Completar pago");

            Assert.True(paid.IsFullyPaid);
            Assert.Equal(4, paid.Payments.Count);
            var reversal = Assert.Single(paid.Payments, x => x.Kind == PaymentKind.Reversal);
            Assert.Equal("Completar pago", reversal.Reason);
            Assert.Equal(2, paid.CurrentCollections.Count);
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Stock_controls_reject_oversell_and_append_adjustment()
    {
        var path = NewPath();
        try
        {
            var (options, employee, product) = await SetupAsync(path, 1);
            var orders = new OrderRepository(new TestFactory(options));
            await orders.SaveOrderAsync(Draft(employee, product));
            await Assert.ThrowsAsync<InvalidOperationException>(() => orders.SaveOrderAsync(Draft(employee, product)));

            var inventory = new InventoryService(new InventoryRepository(new TestFactory(options)));
            await inventory.AdjustAsync(
                product.Id, InventoryMovementType.Incoming, 2, "Recepción de leche", "Proveedor local", null, DateTime.Today);

            await using var db = new CafePosDbContext(options);
            Assert.Equal(2, (await db.Products.SingleAsync()).StockQuantity);
            var movements = await db.InventoryMovements.OrderBy(x => x.OccurredAt).ToListAsync();
            Assert.Equal(2, movements.Count);
            Assert.Equal(InventoryMovementType.Incoming, movements.Last().Type);
            Assert.Equal("Recepción de leche", movements.Last().Reason);
        }
        finally { Delete(path); }
    }

    [Fact]
    public async Task Delivery_requires_normalized_client_details_and_defers_cash_on_delivery()
    {
        var path = NewPath();
        try
        {
            var (options, employee, product) = await SetupAsync(path, 3);
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
            var (options, employee, product) = await SetupAsync(path, 5);
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
            var metrics = await new BusinessMetricsService(
                orders,
                new InventoryRepository(new TestFactory(options))).GetAsync(DateTime.Today, DateTime.Today);
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
            var (options, employee, _) = await SetupAsync(path, 1);
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
            Assert.True(await db.Products.AnyAsync());
        }
        finally { Delete(path); }
    }

    private static async Task<(DbContextOptions<CafePosDbContext> Options, Employee Employee, Product Product)> SetupAsync(string path, int stock)
    {
        var options = new DbContextOptionsBuilder<CafePosDbContext>()
            .UseSqlite($"Data Source={path};Pooling=False").Options;
        var employee = new Employee { DisplayName = "Tester", Role = EmployeeRole.Admin };
        var product = new Product { Name = "Latte", Category = ProductCategory.Coffee, Price = 100m, StockQuantity = stock };
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
