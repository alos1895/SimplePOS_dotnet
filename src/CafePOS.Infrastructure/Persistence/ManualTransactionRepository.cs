using CafePOS.Application.Interfaces;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CafePOS.Infrastructure.Persistence;

public sealed class ManualTransactionRepository(IDbContextFactory<CafePosDbContext> factory) : IManualTransactionRepository
{
    public async Task<IReadOnlyList<ManualTransaction>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.ManualTransactions.AsNoTracking()
            .OrderByDescending(x => x.BusinessDate).ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ManualTransaction>> GetForPeriodAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var start = BusinessDate.FromLocalDate(from);
        var end = BusinessDate.FromLocalDate(to);
        return await db.ManualTransactions.AsNoTracking()
            .Where(x => string.Compare(x.BusinessDate, start) >= 0 && string.Compare(x.BusinessDate, end) < 0)
            .OrderByDescending(x => x.BusinessDate).ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ManualTransaction> AddAsync(ManualTransaction transaction, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        transaction.CreatedAt = transaction.CreatedAt == default ? DateTime.UtcNow : transaction.CreatedAt;
        transaction.BusinessDate = string.IsNullOrWhiteSpace(transaction.BusinessDate)
            ? BusinessDate.FromUtc(transaction.CreatedAt)
            : transaction.BusinessDate;
        db.ManualTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task<ManualTransaction> ReverseAsync(Guid id, string reason, Guid employeeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Escriba el motivo de reversa.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var original = await db.ManualTransactions.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Movimiento no encontrado.");
        if (original.Kind == ManualTransactionKind.Reversal)
            throw new InvalidOperationException("No se puede revertir una reversa.");
        if (await db.ManualTransactions.AnyAsync(x => x.ReversesTransactionId == id, ct))
            throw new InvalidOperationException("El movimiento ya fue revertido.");

        var now = DateTime.UtcNow;
        var reversal = new ManualTransaction
        {
            Concept = $"Reversa: {original.Concept}",
            Amount = original.Amount,
            Type = original.Type == ManualTransactionType.Income ? ManualTransactionType.Expense : ManualTransactionType.Income,
            Kind = ManualTransactionKind.Reversal,
            EmployeeId = employeeId == Guid.Empty ? original.EmployeeId : employeeId,
            BusinessDate = BusinessDate.FromUtc(now),
            ReversesTransactionId = original.Id,
            Reason = reason,
            CreatedAt = now
        };
        db.ManualTransactions.Add(reversal);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return reversal;
    }
}
