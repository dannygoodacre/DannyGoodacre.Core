using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Core;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

public class UserRepository(IdentityContext context) : IUserRepository
{
    public User Add(User user)
        => context.Users
            .Add(user).Entity;

    public Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .AnyAsync(x => x.Username == username, cancellationToken);

    public Task<User?> GetByNameAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    public Task<User?> GetByNameWithTrackingAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
}
