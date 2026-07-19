using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

internal sealed class ClaimRepository(IdentityContext context) : IClaimRepository
{
    public Claim Add(Claim claim)
        => context.Claims
            .Add(claim).Entity;

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Claims
            .AnyAsync(x => x.PublicId == id, cancellationToken);

    public Task<bool> ExistsAsync(string type, string value, CancellationToken cancellationToken = default)
        => context.Claims
            .AnyAsync(x => x.Type == type && x.Value == value, cancellationToken);

    public Task<List<Claim>> GetAllAsync(CancellationToken cancellationToken = default)
        => context.Claims
            .ToListAsync(cancellationToken);

    public Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Claims
            .FirstOrDefaultAsync(x => x.PublicId == id, cancellationToken);

    public Task<Dictionary<Guid, int>> GetIdMappingAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        => context.Claims
            .Where(x => ids.Contains(x.PublicId))
            .ToDictionaryAsync(x => x.PublicId, x => x.Id, cancellationToken);
}
