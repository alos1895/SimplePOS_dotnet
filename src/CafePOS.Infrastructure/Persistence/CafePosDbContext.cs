using CafePOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafePOS.Infrastructure.Persistence;

public sealed class CafePosDbContext(DbContextOptions<CafePosDbContext> options) : DbContext(options)
{
 public DbSet<Product> Products => Set<Product>(); public DbSet<Employee> Employees => Set<Employee>();
 public DbSet<Order> Orders => Set<Order>(); public DbSet<OrderItem> OrderItems => Set<OrderItem>(); public DbSet<Payment> Payments => Set<Payment>();
 public DbSet<CashSession> CashSessions => Set<CashSession>(); public DbSet<CashMovement> CashMovements => Set<CashMovement>();
 protected override void OnModelCreating(ModelBuilder b)
 {
  b.Entity<Product>().Property(x=>x.Price).HasConversion(v=>decimal.ToInt64(decimal.Round(v*100m,MidpointRounding.AwayFromZero)),v=>v/100m);
  b.Entity<Order>().Ignore(x=>x.Subtotal).Ignore(x=>x.Total).Ignore(x=>x.PaidAmount).Ignore(x=>x.BalanceDue).Ignore(x=>x.IsFullyPaid);
  b.Entity<Order>().HasIndex(x=>new{x.CreatedAt,x.DailyNumber}).IsUnique(); b.Entity<Order>().Property(x=>x.Discount).HasConversion(v=>decimal.ToInt64(decimal.Round(v*100m,MidpointRounding.AwayFromZero)),v=>v/100m);
  b.Entity<OrderItem>().Ignore(x=>x.LineTotal).Property(x=>x.UnitPrice).HasConversion(v=>decimal.ToInt64(decimal.Round(v*100m,MidpointRounding.AwayFromZero)),v=>v/100m);
  b.Entity<Payment>().Property(x=>x.Amount).HasConversion(v=>decimal.ToInt64(decimal.Round(v*100m,MidpointRounding.AwayFromZero)),v=>v/100m);
  b.Entity<CashSession>().Ignore(x=>x.ExpectedBalance); b.Entity<CashSession>().Property(x=>x.OpeningAmount).HasConversion(v=>decimal.ToInt64(decimal.Round(v*100m,MidpointRounding.AwayFromZero)),v=>v/100m); b.Entity<CashSession>().Property(x=>x.CountedAmount).HasConversion(v=>decimal.ToInt64(decimal.Round(v*100m,MidpointRounding.AwayFromZero)),v=>v/100m); b.Entity<CashSession>().Property(x=>x.ExpectedAtClose).HasConversion(v=>decimal.ToInt64(decimal.Round(v*100m,MidpointRounding.AwayFromZero)),v=>v/100m);
  b.Entity<CashMovement>().Ignore(x=>x.SignedAmount).Property(x=>x.Amount).HasConversion(v=>decimal.ToInt64(decimal.Round(v*100m,MidpointRounding.AwayFromZero)),v=>v/100m);
  b.Entity<CashMovement>().HasIndex(x=>x.OrderId).IsUnique().HasFilter("OrderId IS NOT NULL");
 }
}
