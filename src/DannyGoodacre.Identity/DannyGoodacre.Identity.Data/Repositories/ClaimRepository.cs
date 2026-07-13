using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Data.Repositories;

internal sealed class ClaimRepository(IdentityContext context) : IClaimRepository
{
    public Claim Add(Claim claim)
        => context.Claims.Add(claim).Entity;
}
