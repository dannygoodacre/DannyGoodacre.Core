using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Abstractions.Services;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface ILoginUser
{
    public Task<Result<int>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}

internal sealed record LoginUserCommand : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed class LoginUserHandler(ILogger<LoginUserHandler> logger,
                                       IStateUnit stateUnit,
                                       IUserRepository repository,
                                       IHashingService hashingService)
    : StateCommandHandler<LoginUserCommand, int>(logger, stateUnit), ILoginUser
{

    protected override string CommandName => "Login User";

    protected async override Task<Result<int>> InternalExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByNameWithTrackingAsync(command.Username, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (!user.IsApproved)
        {
            return DomainError("User not approved");
        }

        if (!hashingService.Verify(command.Password, user.PasswordHash))
        {
            return DomainError("Incorrect password");
        }

        user.SecurityStamp = Guid.NewGuid().ToString();

        user.LastLogin = DateTime.UtcNow;

        return Success(user.Id);
    }

    public Task<Result<int>> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default)
        => ExecuteAsync(new LoginUserCommand
        {
            Username = username,
            Password = password
        }, cancellationToken);
}
