using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CafePOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CafePosDbContext))]
[Migration("20260821000000_InitialDevelopmentSchema")]
public sealed class InitialDevelopmentSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS Employees (
                Id TEXT NOT NULL PRIMARY KEY,
                DisplayName TEXT NOT NULL,
                Role INTEGER NOT NULL,
                IsActive INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Products (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Category INTEGER NOT NULL,
                Price INTEGER NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Products_Category_Name ON Products(Category, Name);
            CREATE TABLE IF NOT EXISTS DeliveryOptions (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Type INTEGER NOT NULL,
                Fee INTEGER NOT NULL,
                IsActive INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_DeliveryOptions_Name ON DeliveryOptions(Name);
            CREATE TABLE IF NOT EXISTS Orders (
                Id TEXT NOT NULL PRIMARY KEY,
                BusinessDate TEXT NOT NULL,
                DailyNumber INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                EmployeeId TEXT NOT NULL,
                Comments TEXT NOT NULL,
                CustomerName TEXT NOT NULL,
                CustomerPhone TEXT NOT NULL,
                DeliveryOptionId TEXT NULL,
                DeliveryOptionName TEXT NOT NULL,
                DeliveryType INTEGER NOT NULL,
                DeliveryStatus INTEGER NOT NULL,
                DeliveryFee INTEGER NOT NULL,
                DeliveryAddress TEXT NOT NULL,
                RiderName TEXT NOT NULL,
                PromisedAt TEXT NULL,
                CashOnDelivery INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                CancelledAt TEXT NULL,
                CancellationReason TEXT NULL,
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Orders_BusinessDate_DailyNumber ON Orders(BusinessDate, DailyNumber);
            CREATE INDEX IF NOT EXISTS IX_Orders_BusinessDate ON Orders(BusinessDate);
            CREATE TABLE IF NOT EXISTS OrderItems (
                Id TEXT NOT NULL PRIMARY KEY,
                OrderId TEXT NOT NULL,
                ProductId TEXT NULL,
                ProductName TEXT NOT NULL,
                Category INTEGER NOT NULL,
                UnitPrice INTEGER NOT NULL,
                Quantity INTEGER NOT NULL,
                Notes TEXT NULL,
                FOREIGN KEY(OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                FOREIGN KEY(ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS IX_OrderItems_OrderId ON OrderItems(OrderId);
            CREATE TABLE IF NOT EXISTS Payments (
                Id TEXT NOT NULL PRIMARY KEY,
                OrderId TEXT NOT NULL,
                BusinessDate TEXT NOT NULL,
                EmployeeId TEXT NOT NULL,
                Method INTEGER NOT NULL,
                Kind INTEGER NOT NULL,
                Amount INTEGER NOT NULL CHECK (Amount > 0),
                Reference TEXT NULL,
                ReversesPaymentId TEXT NULL,
                Reason TEXT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY(OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_Payments_OrderId ON Payments(OrderId);
            CREATE INDEX IF NOT EXISTS IX_Payments_BusinessDate ON Payments(BusinessDate);
            CREATE INDEX IF NOT EXISTS IX_Payments_ReversesPaymentId ON Payments(ReversesPaymentId);
            CREATE TABLE IF NOT EXISTS ManualTransactions (
                Id TEXT NOT NULL PRIMARY KEY,
                Concept TEXT NOT NULL,
                Amount INTEGER NOT NULL CHECK (Amount > 0),
                Type INTEGER NOT NULL,
                Kind INTEGER NOT NULL,
                EmployeeId TEXT NOT NULL,
                BusinessDate TEXT NOT NULL,
                ReversesTransactionId TEXT NULL,
                Reason TEXT NULL,
                CreatedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_ManualTransactions_BusinessDate ON ManualTransactions(BusinessDate);
            CREATE INDEX IF NOT EXISTS IX_ManualTransactions_ReversesTransactionId ON ManualTransactions(ReversesTransactionId);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS ManualTransactions;
            DROP TABLE IF EXISTS Payments;
            DROP TABLE IF EXISTS OrderItems;
            DROP TABLE IF EXISTS Orders;
            DROP TABLE IF EXISTS DeliveryOptions;
            DROP TABLE IF EXISTS Products;
            DROP TABLE IF EXISTS Employees;
            """);
    }
}
