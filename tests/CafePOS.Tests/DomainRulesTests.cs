using Xunit;
using CafePOS.Domain.Entities; using CafePOS.Domain.Enums; using CafePOS.Domain.Services;
namespace CafePOS.Tests;
public sealed class DomainRulesTests
{
 [Fact] public void Totals_use_decimal_and_discount(){var o=OrderWith(3,19.99m);o.Discount=9.97m;Assert.Equal(59.97m,o.Subtotal);Assert.Equal(50.00m,o.Total);}
 [Fact] public void Split_payments_mark_order_fully_paid(){var o=OrderWith(1,100m);o.Payments.Add(new(){Amount=40m,Method=PaymentMethod.Cash});Assert.False(o.IsFullyPaid);o.Payments.Add(new(){Amount=60m,Method=PaymentMethod.Transfer});Assert.True(o.IsFullyPaid);Assert.Equal(0m,o.BalanceDue);}
 [Fact] public void Payment_cannot_exceed_balance(){var o=OrderWith(1,100m);o.Payments.Add(new(){Amount=90m});Assert.Throws<InvalidOperationException>(()=>OrderRules.ValidateForPayment(o,10.01m));}
 [Fact] public void Cash_balance_separates_income_expense_and_sale(){var s=new CashSession{OpenedById=Guid.NewGuid(),OpeningAmount=500m,Movements=[new(){Concept="Venta",Amount=100m,Type=CashMovementType.CashSale},new(){Concept="Insumo",Amount=30m,Type=CashMovementType.ManualExpense},new(){Concept="Cambio",Amount=20m,Type=CashMovementType.ManualIncome}]};Assert.Equal(590m,s.ExpectedBalance);}
 private static Order OrderWith(int quantity,decimal price)=>new(){EmployeeId=Guid.NewGuid(),Items=[new(){ProductName="Latte",Quantity=quantity,UnitPrice=price}]};
}
