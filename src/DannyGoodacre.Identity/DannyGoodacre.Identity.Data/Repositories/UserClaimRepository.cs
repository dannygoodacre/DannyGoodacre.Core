using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

public sealed class UserClaimRepository(IdentityContext context) : IUserClaimRepository
{

    public UserClaim Add(UserClaim userClaim)
        => context.UserClaims
            .Add(userClaim).Entity;

    public Task<HashSet<int>> GetClaimIdsAsync(int userId, CancellationToken cancellationToken)
        => context.UserClaims
            .Where(x => x.UserId == userId)
            .Select(x => x.ClaimId)
            .ToHashSetAsync(cancellationToken);

    public Task<List<Claim>> GetManyAsync(int userId, CancellationToken cancellationToken)
        => context.UserClaims
            .Where(x => x.UserId == userId)
            .Select(x => x.Claim)
            .ToListAsync(cancellationToken);
}
