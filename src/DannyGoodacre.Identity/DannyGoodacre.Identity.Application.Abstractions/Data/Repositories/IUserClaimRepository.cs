using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IUserClaimRepository
{
    UserClaim Add(UserClaim userClaim);
}
