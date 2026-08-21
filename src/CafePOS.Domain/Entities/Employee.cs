using CafePOS.Domain.Enums;

namespace CafePOS.Domain.Entities;

public sealed class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public EmployeeRole Role { get; set; } = EmployeeRole.Cashier;
    public bool IsActive { get; set; } = true;
}
