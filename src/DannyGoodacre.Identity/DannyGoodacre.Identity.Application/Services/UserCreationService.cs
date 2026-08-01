using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Abstractions.Services;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Identity.Application.Services;

internal interface IUserCreationService
{
    Task<Result<User>> CreateUserAsync(string username, string password, CancellationToken cancellationToken = default);
}

internal sealed class UserCreationService(IPasswordHashingService hashingService, IUserRepository repository) : IUserCreationService
{
    public async Task<Result<User>> CreateUserAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsAsync(username, cancellationToken))
        {
            return Result<User>.Conflict("Username already exists");
        }

        string passwordHash = hashingService.Hash(password);

        User user = repository.Add(new User
        {
            PublicId = Guid.NewGuid(),
            Username = username,
            IsApproved = false,
            PasswordHash = passwordHash,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });

        return Result.Success(user);
    }
}
