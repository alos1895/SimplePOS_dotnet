using CafePOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CafePOS.Infrastructure.Persistence;

public sealed class CafePosDbContext(DbContextOptions<CafePosDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<DeliveryOption> DeliveryOptions => Set<DeliveryOption>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ManualTransaction> ManualTransactions => Set<ManualTransaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var cents = new ValueConverter<decimal, long>(
            value => decimal.ToInt64(decimal.Round(value * 100m, MidpointRounding.AwayFromZero)),
            value => value / 100m);

        builder.Entity<Product>().HasIndex(x => new { x.Category, x.Name }).IsUnique();
        builder.Entity<Product>().Property(x => x.Price).HasConversion(cents);
        builder.Entity<Product>().Property(x => x.StockQuantity).IsConcurrencyToken();
        builder.Entity<Product>().ToTable(x =>
        {
            x.HasCheckConstraint("CK_Products_StockQuantity", "StockQuantity >= 0");
            x.HasCheckConstraint("CK_Products_LowStockThreshold", "LowStockThreshold >= 0");
        });

        builder.Entity<InventoryMovement>().HasIndex(x => new { x.BusinessDate, x.ProductId });
        builder.Entity<InventoryMovement>().HasIndex(x => x.OrderId);
        builder.Entity<InventoryMovement>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DeliveryOption>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<DeliveryOption>().Property(x => x.Fee).HasConversion(cents);

        builder.Entity<Order>().Ignore(x => x.Subtotal).Ignore(x => x.Total).Ignore(x => x.PaidAmount)
            .Ignore(x => x.BalanceDue).Ignore(x => x.IsFullyPaid).Ignore(x => x.CurrentCollections);
        builder.Entity<Order>().HasIndex(x => new { x.BusinessDate, x.DailyNumber }).IsUnique();
        builder.Entity<Order>().Property(x => x.DeliveryFee).HasConversion(cents);
        builder.Entity<OrderItem>().Ignore(x => x.LineTotal).Property(x => x.UnitPrice).HasConversion(cents);
        builder.Entity<OrderItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Payment>().Property(x => x.Amount).HasConversion(cents);
        builder.Entity<Payment>().HasIndex(x => x.BusinessDate);
        builder.Entity<Payment>().HasIndex(x => x.ReversesPaymentId);
        builder.Entity<ManualTransaction>().Property(x => x.Amount).HasConversion(cents);
        builder.Entity<ManualTransaction>().HasIndex(x => x.BusinessDate);
        builder.Entity<ManualTransaction>().HasIndex(x => x.ReversesTransactionId);
    }
}
