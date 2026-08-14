using CafePOS.Domain.Enums;

namespace CafePOS.Domain.Entities;

public sealed class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string DisplayName { get; set; }
    public EmployeeRole Role { get; set; } = EmployeeRole.Cashier;
    public bool IsActive { get; set; } = true;
}
