using CafePOS.Domain.Enums;

namespace CafePOS.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public ProductCategory Category { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class DeliveryOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public DeliveryType Type { get; set; }
    public decimal Fee { get; set; }
    public bool IsActive { get; set; } = true;
}
