using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain.Entities;
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

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Users
            .Include(x => x.Claims)
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.PublicId == id, cancellationToken);

    public Task<User?> GetAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .Include(x => x.Claims)
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    public Task<User?> GetWithTrackingAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Users
            .AsTracking()
            .FirstOrDefaultAsync(x => x.PublicId == id, cancellationToken);

    public Task<User?> GetWithTrackingAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    public void Remove(User user)
        => context.Users
            .Remove(user);
}
