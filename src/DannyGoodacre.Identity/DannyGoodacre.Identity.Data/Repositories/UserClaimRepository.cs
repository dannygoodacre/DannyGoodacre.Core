using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Data.Repositories;

public sealed class UserClaimRepository(IdentityContext context) : IUserClaimRepository
{

    public UserClaim Add(UserClaim userClaim)
        => context.UserClaims
            .Add(userClaim).Entity;
}
