using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CafePOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CafePosDbContext))]
[Migration("20260817010000_AddGloriaDrinkMenu")]
public sealed class AddGloriaDrinkMenu : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO Products (Id, Name, Category, Price, StockQuantity, LowStockThreshold, IsActive, CreatedAt, UpdatedAt) VALUES
        ('30000000-0000-0000-0000-000000000001', 'Café latte', 0, 65, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000002', 'Capuchino', 0, 65, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000003', 'Café mocha', 0, 65, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000004', 'Café americano', 0, 55, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000005', 'Café de olla', 0, 55, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000006', 'Tisana caliente', 0, 65, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000007', 'Chocolate caliente', 0, 65, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000008', 'Taro caliente', 0, 65, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000009', 'Matcha caliente', 0, 65, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000010', 'Chai caliente', 0, 65, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000011', 'Bebida caliente de temporada', 0, 70, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000012', 'Café latte a las rocas', 1, 75, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000013', 'Tisana helada', 1, 75, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000014', 'Chocolate helado', 1, 75, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000015', 'Taro helado', 1, 75, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000016', 'Matcha helado', 1, 75, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000017', 'Chai helado', 1, 75, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000018', 'Cold brew', 1, 75, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000019', 'Frapuchino', 1, 78, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000020', 'Botella de agua 500 ml', 1, 16, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('30000000-0000-0000-0000-000000000021', 'Bebida helada de temporada', 1, 78, 30, 5, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        ON CONFLICT(Category, Name) DO UPDATE SET
            Price = excluded.Price,
            StockQuantity = excluded.StockQuantity,
            LowStockThreshold = excluded.LowStockThreshold,
            IsActive = 1,
            UpdatedAt = CURRENT_TIMESTAMP;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DELETE FROM Products WHERE Id LIKE '30000000-0000-0000-0000-%';");
}
