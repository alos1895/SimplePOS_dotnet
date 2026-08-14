using CafePOS.Domain.Enums;

namespace CafePOS.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public ProductCategory Category { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class InventoryMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? OrderId { get; set; }
    public Guid EmployeeId { get; set; }
    public string BusinessDate { get; set; } = Services.BusinessDate.Today;
    public InventoryMovementType Type { get; set; }
    public int QuantityDelta { get; set; }
    public required string Reason { get; set; }
    public string? Supplier { get; set; }
    public string? Reference { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public sealed class DeliveryOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public DeliveryType Type { get; set; }
    public decimal Fee { get; set; }
    public bool IsActive { get; set; } = true;
}
