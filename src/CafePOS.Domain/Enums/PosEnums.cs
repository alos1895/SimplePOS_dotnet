namespace CafePOS.Domain.Enums;

public enum OrderStatus { Open, Paid, Cancelled }
public enum PaymentMethod { Cash, Card }
public enum PaymentKind { Collection, Reversal, Refund }
public enum ProductCategory { Coffee, Beverages, Food, Desserts, Extras, Combos }
public enum DeliveryType { Pickup, Walking, Delivery }
public enum DeliveryStatus { None, Preparing, OutForDelivery, Delivered, Cancelled }
public enum InventoryMovementType { Incoming, Count, Waste, Correction, Sale, Cancellation }
public enum ManualTransactionType { Income, Expense }
public enum ManualTransactionKind { Entry, Reversal }
public enum EmployeeRole { Admin, Cashier }
