using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

internal sealed class ClaimRepository(IdentityContext context) : IClaimRepository
{
    public Claim Add(Claim claim)
        => context.Claims
            .Add(claim).Entity;

    public Task<bool> ExistsAsync(Guid publicId, CancellationToken cancellationToken = default)
        => context.Claims
            .AnyAsync(x => x.PublicId == publicId, cancellationToken);

    public Task<bool> ExistsAsync(string type, string value, CancellationToken cancellationToken = default)
        => context.Claims
            .AnyAsync(x => x.Type == type && x.Value == value, cancellationToken);

    public Task<List<Claim>> GetAllAsync(CancellationToken cancellationToken = default)
        => context.Claims
            .ToListAsync(cancellationToken);

    public Task<Claim?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        => context.Claims
            .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);

    public Task<Dictionary<Guid, int>> GetIdMapAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        => context.Claims
            .Where(x => ids.Contains(x.PublicId))
            .ToDictionaryAsync(x => x.PublicId, x => x.Id, cancellationToken);
}
