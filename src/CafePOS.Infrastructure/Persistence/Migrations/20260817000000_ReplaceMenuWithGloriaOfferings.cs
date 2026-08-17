using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CafePOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CafePosDbContext))]
[Migration("20260817000000_ReplaceMenuWithGloriaOfferings")]
public sealed class ReplaceMenuWithGloriaOfferings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep historical products for order and inventory audit trails, but remove
        // them from the active POS catalog before installing the new menu.
        migrationBuilder.Sql("UPDATE Products SET IsActive = 0, UpdatedAt = CURRENT_TIMESTAMP;");
        migrationBuilder.Sql(MenuInsertSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM Products WHERE Id LIKE '20000000-0000-0000-0000-%';");
        migrationBuilder.Sql("UPDATE Products SET IsActive = 1, UpdatedAt = CURRENT_TIMESTAMP;");
    }

    private const string MenuInsertSql = """
        INSERT INTO Products (Id, Name, Category, Price, StockQuantity, LowStockThreshold, IsActive, CreatedAt, UpdatedAt) VALUES
        ('20000000-0000-0000-0000-000000000001', 'Latte + muffin', 0, 99, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000002', 'Capuchino + muffin', 0, 99, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000003', 'Chocolate caliente + muffin', 0, 99, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000004', 'Panini de jamón con queso manchego + bebida', 2, 149, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000005', 'Panini de pollo con queso manchego + bebida', 2, 159, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000006', 'Croissant clásico', 2, 69, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000007', 'Croissant clásico + bebida', 2, 119, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000008', 'Croissant italiano', 2, 69, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000009', 'Croissant italiano + bebida', 2, 119, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000010', 'Croissant saludable', 2, 69, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000011', 'Croissant saludable + bebida', 2, 119, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000012', 'Croissant de fresa', 3, 69, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000013', 'Croissant de fresa + bebida', 3, 119, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000014', 'Croissant de manzana y canela', 3, 69, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000015', 'Croissant de manzana y canela + bebida', 3, 119, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000016', 'Croissant cajetoso', 3, 69, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('20000000-0000-0000-0000-000000000017', 'Croissant cajetoso + bebida', 3, 119, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        ON CONFLICT(Category, Name) DO UPDATE SET
            Price = excluded.Price,
            StockQuantity = excluded.StockQuantity,
            LowStockThreshold = excluded.LowStockThreshold,
            IsActive = 1,
            UpdatedAt = CURRENT_TIMESTAMP;
        """;
}
