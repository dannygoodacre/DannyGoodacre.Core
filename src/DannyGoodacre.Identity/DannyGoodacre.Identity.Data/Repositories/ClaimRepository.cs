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
}
