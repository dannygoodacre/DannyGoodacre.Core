using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IClaimRepository
{
    Claim Add(Claim claim);
}
