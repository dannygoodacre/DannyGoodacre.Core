using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

internal sealed class ClaimRepository(IdentityContext context) : IClaimRepository
{
    public Claim Add(Claim claim)
        => context.Claims
            .Add(claim).Entity;

    public Task<List<Claim>> GetAllAsync(CancellationToken cancellationToken = default)
        => context.Claims
            .ToListAsync(cancellationToken);

    public Task<Claim?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        => context.Claims
            .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);

    public Task<List<Claim>> GetExistingAsync(List<ClaimDefinition> claims, CancellationToken cancellationToken = default)
    {
        if (claims.Count == 0)
        {
            return Task.FromResult(new List<Claim>());
        }

        return context.Claims
            .Where(x => claims.Any(claim => claim.Type == x.Type && claim.Value == x.Value))
            .ToListAsync(cancellationToken);
    }

    public Task<Dictionary<Guid, int>> GetIdMapAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        => context.Claims
            .Where(x => ids.Contains(x.PublicId))
            .ToDictionaryAsync(x => x.PublicId, x => x.Id, cancellationToken);
}
