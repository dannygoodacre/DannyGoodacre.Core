using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

public class UserRepository(IdentityContext context) : IUserRepository
{
    public User Add(User user)
        => context.Users
            .Add(user).Entity;

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Users
            .AnyAsync(x => x.PublicId == id, cancellationToken);

    public Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .AnyAsync(x => x.Username == username, cancellationToken);

    public Task<User?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
        => context.Users
            .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);

    public Task<User?> GetAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    public Task<User?> GetWithTrackingAsync(Guid publicId, CancellationToken cancellationToken = default)
        => context.Users
            .AsTracking()
            .FirstOrDefaultAsync(x => x.PublicId == publicId, cancellationToken);

    public Task<User?> GetWithTrackingAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    public void Remove(User user)
        => context.Users
            .Remove(user);
}
