using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Core;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

public class RoleRepository(IdentityContext context) : IRoleRepository
{
    public void Add(string name)
        => context.Roles.Add(new Role { Name = name });

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
        => context.Roles
            .AnyAsync(role => role.Name == name, cancellationToken);
}
