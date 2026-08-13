using CafePOS.Application.Interfaces;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
namespace CafePOS.Application.Services;

public sealed class CashService(IPosStore store)
{
    public async Task<CashSession> OpenAsync(Guid employeeId, int register, decimal opening, CancellationToken ct = default)
    {
        if (opening < 0) throw new ArgumentOutOfRangeException(nameof(opening));
        if (await store.GetOpenCashSessionAsync(ct) is not null) throw new InvalidOperationException("Ya existe una caja abierta.");
        return await store.SaveCashSessionAsync(new CashSession { OpenedById = employeeId, RegisterNumber = register, OpeningAmount = opening }, ct);
    }
    public async Task<CashSession> AddMovementAsync(CashMovementType type, decimal amount, string concept, Guid? orderId = null, CancellationToken ct = default)
    {
        var session = await store.GetOpenCashSessionAsync(ct) ?? throw new InvalidOperationException("Debe abrir la caja primero.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (type is CashMovementType.Opening or CashMovementType.ClosingAdjustment) throw new InvalidOperationException("Tipo reservado.");
        session.Movements.Add(new CashMovement { CashSessionId = session.Id, Type = type, Amount = amount, Concept = concept.Trim(), OrderId = orderId });
        return await store.SaveCashSessionAsync(session, ct);
    }
    public async Task<CashSession> CloseAsync(decimal counted, CancellationToken ct = default)
    {
        var session = await store.GetOpenCashSessionAsync(ct) ?? throw new InvalidOperationException("No existe una caja abierta.");
        if (counted < 0) throw new ArgumentOutOfRangeException(nameof(counted));
        session.ExpectedAtClose = session.ExpectedBalance; session.CountedAmount = counted;
        session.ClosedAt = DateTimeOffset.UtcNow; session.Status = CashSessionStatus.Closed;
        return await store.SaveCashSessionAsync(session, ct);
    }
}
