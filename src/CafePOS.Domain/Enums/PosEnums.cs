namespace CafePOS.Domain.Enums;

public enum OrderStatus { Open, Paid, Cancelled }
public enum PaymentMethod { Cash, Transfer, Card, Other }
public enum CashSessionStatus { Open, Closed }
public enum CashMovementType { Opening, ManualIncome, ManualExpense, CashSale, Refund, ClosingAdjustment }
