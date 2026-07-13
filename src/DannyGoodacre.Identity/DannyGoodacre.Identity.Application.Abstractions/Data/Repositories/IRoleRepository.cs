using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IRoleRepository
{
    Role Add(string name);

    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
