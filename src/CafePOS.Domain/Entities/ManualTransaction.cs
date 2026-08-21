using CafePOS.Domain.Enums;

namespace CafePOS.Domain.Entities;

public sealed class ManualTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Concept { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ManualTransactionType Type { get; set; }
    public ManualTransactionKind Kind { get; set; } = ManualTransactionKind.Entry;
    public Guid EmployeeId { get; set; }
    public string BusinessDate { get; set; } = Services.BusinessDate.Today;
    public Guid? ReversesTransactionId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string TypeLabel => Type == ManualTransactionType.Income ? "INGRESO" : "GASTO";
}
