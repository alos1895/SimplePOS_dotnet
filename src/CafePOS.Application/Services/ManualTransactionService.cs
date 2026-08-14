using CafePOS.Application.Interfaces;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;

namespace CafePOS.Application.Services;

public sealed class ManualTransactionService(IManualTransactionRepository transactions, ICurrentUserContext? user = null)
{
    public Task<IReadOnlyList<ManualTransaction>> GetAllAsync(CancellationToken ct = default) =>
        transactions.GetAllAsync(ct);

    public Task ReverseAsync(Guid id, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Escriba el motivo de reversa.");
        return transactions.ReverseAsync(id, reason.Trim(), user?.Current.EmployeeId ?? Guid.Empty, ct);
    }

    public Task<ManualTransaction> CreateAsync(
        string concept,
        decimal amount,
        ManualTransactionType type,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(concept))
            throw new InvalidOperationException("Escriba el concepto del movimiento.");
        if (amount <= 0)
            throw new InvalidOperationException("El monto debe ser mayor que cero.");
        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
            throw new InvalidOperationException("El monto solo puede tener dos decimales.");

        return transactions.AddAsync(new ManualTransaction
        {
            Concept = concept.Trim(),
            Amount = amount,
            Type = type,
            EmployeeId = user?.Current.EmployeeId ?? Guid.Empty
        }, ct);
    }
}
