using CafePOS.Domain.Enums;
namespace CafePOS.Domain.Entities;

public sealed class CashSession
{
    public Guid Id { get; set; } = Guid.NewGuid(); public int RegisterNumber { get; set; } = 1;
    public Guid OpenedById { get; set; } public Employee? OpenedBy { get; set; }
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; } public decimal OpeningAmount { get; set; }
    public decimal? CountedAmount { get; set; } public decimal? ExpectedAtClose { get; set; }
    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;
    public List<CashMovement> Movements { get; set; } = [];
    public decimal ExpectedBalance => OpeningAmount + Movements.Sum(x => x.SignedAmount);
}

public sealed class CashMovement
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid CashSessionId { get; set; }
    public CashSession? CashSession { get; set; } public CashMovementType Type { get; set; }
    public decimal Amount { get; set; } public required string Concept { get; set; }
    public Guid? OrderId { get; set; } public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public decimal SignedAmount => Type is CashMovementType.ManualExpense or CashMovementType.Refund ? -Amount : Amount;
}
