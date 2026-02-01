using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IUserRepository
{
    void Add(User user);

    Task ApproveAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetForUpdateAsync(string username, CancellationToken cancellationToken = default);
}
