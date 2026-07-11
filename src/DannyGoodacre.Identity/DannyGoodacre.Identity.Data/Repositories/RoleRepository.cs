using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Core;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

public class RoleRepository(IdentityContext context) : IRoleRepository
{
    public Role Add(string name)
        => context.Roles
            .Add(new Role { Name = name }).Entity;

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
        => context.Roles
            .AnyAsync(role => role.Name == name, cancellationToken);
}
