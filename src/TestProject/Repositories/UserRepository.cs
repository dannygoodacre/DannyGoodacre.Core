using Microsoft.EntityFrameworkCore;

namespace TestProject.Repositories;

internal interface IUserRepository
{
    void Add(User user);

    Task<User?> GetAsync(string name, CancellationToken cancellationToken);
}

internal sealed class UserRepository(IdentityContext context) : IUserRepository
{

    public void Add(User user)
        => context.Add(user);

    public Task<User?> GetAsync(string name, CancellationToken cancellationToken)
        => context.Users.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
}
