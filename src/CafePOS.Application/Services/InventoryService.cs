using CafePOS.Application.Interfaces;
using CafePOS.Application.Models;
using CafePOS.Domain.Enums;

namespace CafePOS.Application.Services;

public sealed class InventoryService(IInventoryRepository inventory, ICurrentUserContext? user = null)
{
    public Task<InventorySnapshot> GetSnapshotAsync(DateTime day, CancellationToken ct = default) =>
        inventory.GetSnapshotAsync(day, ct);

    public async Task AdjustAsync(
        Guid productId,
        InventoryMovementType type,
        int quantity,
        string reason,
        string? supplier,
        string? reference,
        DateTime effectiveDate,
        CancellationToken ct = default)
    {
        if (productId == Guid.Empty) throw new InvalidOperationException("Seleccione un producto.");
        RequireAdmin();
        if (type is InventoryMovementType.Sale or InventoryMovementType.Cancellation)
            throw new InvalidOperationException("Este tipo de movimiento se genera automáticamente.");
        if (quantity < 0 && type is (InventoryMovementType.Incoming or InventoryMovementType.Count))
            throw new InvalidOperationException("La entrada y el conteo no pueden ser negativos.");
        if (type == InventoryMovementType.Waste && quantity <= 0)
            throw new InvalidOperationException("La merma debe capturarse como una cantidad positiva.");
        if (type != InventoryMovementType.Count && quantity == 0)
            throw new InvalidOperationException("Capture una cantidad diferente de cero.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Escriba el motivo del movimiento.");
        if (type == InventoryMovementType.Incoming && string.IsNullOrWhiteSpace(supplier) && string.IsNullOrWhiteSpace(reference))
            throw new InvalidOperationException("La entrada requiere proveedor o referencia.");
        await inventory.AdjustAsync(new StockAdjustment(
            productId, type, quantity, reason.Trim(), supplier?.Trim(), reference?.Trim(), effectiveDate,
            user?.Current.EmployeeId ?? Guid.Empty), ct);
    }

    private void RequireAdmin()
    {
        if (user is not null && user.Current.Role != EmployeeRole.Admin)
            throw new UnauthorizedAccessException("Solo un administrador puede ajustar el inventario.");
    }
}
