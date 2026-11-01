namespace DannyGoodacre.Cqrs.Examples;

public interface IUserRepository
{
    void Add(User user);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class UserRepository : IUserRepository
{
    public void Add(User user)
    {
        Console.WriteLine("User added");
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("User fetched");

        return Task.FromResult(new User(id, "email", "name"))!;
    }
}
