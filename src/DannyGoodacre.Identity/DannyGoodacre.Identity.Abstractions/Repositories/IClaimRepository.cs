using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Abstractions.Repositories;

public interface IClaimRepository
{
    Claim Add(Claim claim);

    Task<List<Claim>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Claim?> GetAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<List<Claim>> GetExistingAsync(List<ClaimDefinition> claims, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetIdMapAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
