using CafePOS.Application.Interfaces;
using CafePOS.Application.Models;

namespace CafePOS.Application.Services;

public sealed class AdminCatalogService(IAdminCatalogRepository catalog, ICurrentUserContext? user = null)
{
    public Task<AdminCatalogData> GetAsync(CancellationToken ct = default)
    {
        RequireAdmin();
        return catalog.GetAsync(ct);
    }

    public async Task SaveProductAsync(ProductUpsert product, CancellationToken ct = default)
    {
        RequireAdmin();
        ValidateName(product.Name);
        if (product.Price <= 0) throw new InvalidOperationException("El precio debe ser mayor que cero.");
        ValidateMoney(product.Price, "El precio");
        if (product.InitialStockQuantity < 0) throw new InvalidOperationException("El inventario inicial no puede ser negativo.");
        if (product.LowStockThreshold < 0) throw new InvalidOperationException("El nivel de alerta no puede ser negativo.");
        await catalog.SaveProductAsync(product with { Name = product.Name.Trim() }, user?.Current.EmployeeId ?? Guid.Empty, ct);
    }

    public Task DeactivateProductAsync(Guid id, CancellationToken ct = default)
    {
        RequireAdmin();
        return catalog.DeactivateProductAsync(id, ct);
    }

    public async Task SaveDeliveryOptionAsync(DeliveryOptionUpsert option, CancellationToken ct = default)
    {
        RequireAdmin();
        ValidateName(option.Name);
        if (option.Fee < 0) throw new InvalidOperationException("La tarifa no puede ser negativa.");
        ValidateMoney(option.Fee, "La tarifa");
        await catalog.SaveDeliveryOptionAsync(option with { Name = option.Name.Trim() }, ct);
    }

    public Task DeactivateDeliveryOptionAsync(Guid id, CancellationToken ct = default)
    {
        RequireAdmin();
        return catalog.DeactivateDeliveryOptionAsync(id, ct);
    }

    private void RequireAdmin()
    {
        if (user is not null && user.Current.Role != Domain.Enums.EmployeeRole.Admin)
            throw new UnauthorizedAccessException("Solo un administrador puede cambiar el catálogo.");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Escriba un nombre.");
    }

    private static void ValidateMoney(decimal amount, string label)
    {
        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
            throw new InvalidOperationException($"{label} solo puede tener dos decimales.");
    }
}
