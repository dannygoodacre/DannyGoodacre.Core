
using DannyGoodacre.Identity.Entities;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IClaimRepository
{
    Claim Add(Claim claim);

    Task<bool> ExistsAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string type, string value, CancellationToken cancellationToken = default);

    Task<List<Claim>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Claim?> GetAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetIdMapAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
