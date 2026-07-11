using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IUserRepository
{
    User Add(User user);

    Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetWithTrackingAsync(string username, CancellationToken cancellationToken = default);
}
