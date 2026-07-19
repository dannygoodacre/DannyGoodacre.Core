using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IRoleRepository
{
    Role Add(Role role);

    void Remove(Role role);

    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);

    Task<Role?> GetAsync(Guid publicId, CancellationToken cancellationToken = default);
}
