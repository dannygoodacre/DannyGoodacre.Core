using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IUserRepository
{
    User Add(User user);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetWithTrackingAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetWithTrackingAsync(string username, CancellationToken cancellationToken = default);

    void Remove(User user);
}
