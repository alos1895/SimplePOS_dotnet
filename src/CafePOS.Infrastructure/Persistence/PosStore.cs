using CafePOS.Application.Interfaces; using CafePOS.Domain.Entities; using CafePOS.Domain.Enums; using Microsoft.EntityFrameworkCore;
namespace CafePOS.Infrastructure.Persistence;

public sealed class PosStore(IDbContextFactory<CafePosDbContext> factory) : IPosStore
{
 public async Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken ct=default) { await using var db=await factory.CreateDbContextAsync(ct); return await db.Products.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Category).ThenBy(x=>x.Name).ToListAsync(ct); }
 public async Task<IReadOnlyList<Order>> GetOrdersAsync(DateTimeOffset from,DateTimeOffset to,string? search,CancellationToken ct=default) { await using var db=await factory.CreateDbContextAsync(ct); var q=db.Orders.AsNoTracking().Include(x=>x.Items).Include(x=>x.Payments).Include(x=>x.Employee).Where(x=>x.CreatedAt>=from&&x.CreatedAt<to); if(!string.IsNullOrWhiteSpace(search)) q=q.Where(x=>x.DailyNumber.ToString().Contains(search)||x.Items.Any(i=>i.ProductName.Contains(search))); return await q.OrderByDescending(x=>x.CreatedAt).ToListAsync(ct); }
 public async Task<Order?> GetOrderAsync(Guid id,CancellationToken ct=default) { await using var db=await factory.CreateDbContextAsync(ct); return await db.Orders.Include(x=>x.Items).Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==id,ct); }
 public async Task<Order> SaveOrderAsync(Order order,CancellationToken ct=default)
 {
  await using var db=await factory.CreateDbContextAsync(ct); await using var tx=await db.Database.BeginTransactionAsync(ct);
  var exists=await db.Orders.AnyAsync(x=>x.Id==order.Id,ct);
  if(!exists) { var start=new DateTimeOffset(order.CreatedAt.Year,order.CreatedAt.Month,order.CreatedAt.Day,0,0,0,order.CreatedAt.Offset); var end=start.AddDays(1); order.DailyNumber=(await db.Orders.Where(x=>x.CreatedAt>=start&&x.CreatedAt<end).MaxAsync(x=>(int?)x.DailyNumber,ct)??0)+1; db.Orders.Add(order); }
  else { var paymentIds=await db.Payments.Where(x=>x.OrderId==order.Id).Select(x=>x.Id).ToListAsync(ct); db.Orders.Update(order); foreach(var payment in order.Payments.Where(x=>!paymentIds.Contains(x.Id))) db.Entry(payment).State=EntityState.Added; }
  if(order.Status==OrderStatus.Paid)
  {
   var cash=order.Payments.Where(x=>x.Method==PaymentMethod.Cash).Sum(x=>x.Amount);
   if(cash>0&&!await db.CashMovements.AnyAsync(x=>x.OrderId==order.Id,ct)) { var session=await db.CashSessions.SingleOrDefaultAsync(x=>x.Status==CashSessionStatus.Open,ct)??throw new InvalidOperationException("Debe abrir la caja antes de cobrar en efectivo."); db.CashMovements.Add(new CashMovement{CashSessionId=session.Id,Type=CashMovementType.CashSale,Amount=cash,Concept=$"Venta #{order.DailyNumber}",OrderId=order.Id}); }
  }
  await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return order;
 }
 public async Task<CashSession?> GetOpenCashSessionAsync(CancellationToken ct=default) { await using var db=await factory.CreateDbContextAsync(ct); return await db.CashSessions.Include(x=>x.Movements).SingleOrDefaultAsync(x=>x.Status==CashSessionStatus.Open,ct); }
 public async Task<CashSession> SaveCashSessionAsync(CashSession s,CancellationToken ct=default) { await using var db=await factory.CreateDbContextAsync(ct); if(await db.CashSessions.AnyAsync(x=>x.Id==s.Id,ct)) db.Update(s); else db.Add(s); await db.SaveChangesAsync(ct); return s; }
}
