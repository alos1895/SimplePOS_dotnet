using CafePOS.Application.Interfaces;
using CafePOS.Domain.Enums;

namespace CafePOS.Infrastructure.Persistence;

// This is an explicit local-workstation scope, not an authentication mechanism.
public sealed class CurrentUserContext : ICurrentUserContext
{
    public CurrentUser Current { get; } = new(Seed.DefaultEmployeeId, "Administrador", EmployeeRole.Admin);
}
