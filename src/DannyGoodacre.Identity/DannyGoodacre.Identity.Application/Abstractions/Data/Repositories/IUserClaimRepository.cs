using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IUserClaimRepository
{
    UserClaim Add(UserClaim userClaim);

    Task<HashSet<int>> GetClaimIdsAsync(int userId, CancellationToken cancellationToken);

    Task<List<Claim>> GetManyAsync(int userId, CancellationToken cancellationToken);
}
