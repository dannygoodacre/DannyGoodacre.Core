using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Core;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data.Repositories;

public class UserRepository(IdentityContext context) : IUserRepository
{
    public void Add(User user)
        => context.Users
            .Add(user);

    public Task ApproveAsync(string username, CancellationToken cancellationToken)
        => context.Users
            .ExecuteUpdateAsync(
                x => x.SetProperty(
                    user => user.Username == username,
                    user => user.IsApproved == true),
                cancellationToken);

    public Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .AnyAsync(user => user.Username == username, cancellationToken);

    public Task<User?> GetForUpdateAsync(string username, CancellationToken cancellationToken = default)
        => context.Users
            .AsTracking()
            .FirstOrDefaultAsync(
                user => user.Username == username,
                cancellationToken);
}
