using DannyGoodacre.Identity.Application.Abstractions.Repositories;
using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

public class RoleRepository(IdentityContext context) : IRoleRepository
{
    public Role Add(Role role)
        => context.Roles
            .Add(role).Entity;

    public void Remove(Role role)
        => context.Roles
            .Remove(role);

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
        => context.Roles
            .AnyAsync(x => x.Name == name, cancellationToken);

    public Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        => context.Roles
            .Include(x => x.Claims)
            .ThenInclude(x => x.Claim)
            .ToListAsync(cancellationToken);

    public Task<Role?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        => context.Roles
            .Include(x => x.Claims)
            .ThenInclude(x => x.Claim)
            .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);
}
