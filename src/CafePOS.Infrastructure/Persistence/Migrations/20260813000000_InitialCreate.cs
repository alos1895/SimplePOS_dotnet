using Microsoft.EntityFrameworkCore.Migrations;
namespace CafePOS.Infrastructure.Persistence.Migrations;
[Migration("20260813000000_InitialCreate")]
public sealed class InitialCreate : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.Sql("""
CREATE TABLE Employees (Id TEXT NOT NULL PRIMARY KEY, DisplayName TEXT NOT NULL, IsActive INTEGER NOT NULL);
CREATE TABLE Products (Id TEXT NOT NULL PRIMARY KEY, Name TEXT NOT NULL, Category TEXT NOT NULL, Price INTEGER NOT NULL, IsActive INTEGER NOT NULL, ImagePath TEXT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL);
CREATE TABLE Orders (Id TEXT NOT NULL PRIMARY KEY, DailyNumber INTEGER NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL, EmployeeId TEXT NOT NULL, Comments TEXT NOT NULL, Discount INTEGER NOT NULL, Status INTEGER NOT NULL, CancelledAt TEXT NULL, CancellationReason TEXT NULL, FOREIGN KEY(EmployeeId) REFERENCES Employees(Id));
CREATE UNIQUE INDEX IX_Orders_CreatedAt_DailyNumber ON Orders(CreatedAt, DailyNumber);
CREATE TABLE OrderItems (Id TEXT NOT NULL PRIMARY KEY, OrderId TEXT NOT NULL, ProductId TEXT NULL, ProductName TEXT NOT NULL, UnitPrice INTEGER NOT NULL, Quantity INTEGER NOT NULL, Notes TEXT NULL, FOREIGN KEY(OrderId) REFERENCES Orders(Id) ON DELETE CASCADE, FOREIGN KEY(ProductId) REFERENCES Products(Id));
CREATE TABLE Payments (Id TEXT NOT NULL PRIMARY KEY, OrderId TEXT NOT NULL, Method INTEGER NOT NULL, Amount INTEGER NOT NULL, Reference TEXT NULL, CreatedAt TEXT NOT NULL, FOREIGN KEY(OrderId) REFERENCES Orders(Id) ON DELETE CASCADE);
CREATE TABLE CashSessions (Id TEXT NOT NULL PRIMARY KEY, RegisterNumber INTEGER NOT NULL, OpenedById TEXT NOT NULL, OpenedAt TEXT NOT NULL, ClosedAt TEXT NULL, OpeningAmount INTEGER NOT NULL, CountedAmount INTEGER NULL, ExpectedAtClose INTEGER NULL, Status INTEGER NOT NULL, FOREIGN KEY(OpenedById) REFERENCES Employees(Id));
CREATE TABLE CashMovements (Id TEXT NOT NULL PRIMARY KEY, CashSessionId TEXT NOT NULL, Type INTEGER NOT NULL, Amount INTEGER NOT NULL, Concept TEXT NOT NULL, OrderId TEXT NULL, CreatedAt TEXT NOT NULL, FOREIGN KEY(CashSessionId) REFERENCES CashSessions(Id) ON DELETE CASCADE);
CREATE UNIQUE INDEX IX_CashMovements_OrderId ON CashMovements(OrderId) WHERE OrderId IS NOT NULL;
""");
 }
 protected override void Down(MigrationBuilder m) { m.Sql("DROP TABLE CashMovements; DROP TABLE Payments; DROP TABLE OrderItems; DROP TABLE CashSessions; DROP TABLE Orders; DROP TABLE Products; DROP TABLE Employees;"); }
}
