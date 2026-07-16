using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IClaimRepository
{
    Claim Add(Claim claim);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string type, string value, CancellationToken cancellationToken = default);

    Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
