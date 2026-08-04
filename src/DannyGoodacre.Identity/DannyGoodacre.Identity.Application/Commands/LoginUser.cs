using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface ILoginUser
{
    public Task<Result<Guid>> ExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default);
}

public sealed record LoginUserCommand : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed class LoginUserHandler(ILogger<LoginUserHandler> logger,
                                       IStateUnit stateUnit,
                                       IUserRepository repository,
                                       IPasswordHashingService hashingService)
    : StateCommandHandler<LoginUserCommand, Guid>(logger, stateUnit), ILoginUser
{

    protected override string CommandName => "Login User";

    protected async override Task<Result<Guid>> InternalExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetWithTrackingAsync(command.Username, cancellationToken);

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

        return Success(user.PublicId);
    }

    public new Task<Result<Guid>> ExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
        => base.ExecuteAsync(command, cancellationToken);
}
