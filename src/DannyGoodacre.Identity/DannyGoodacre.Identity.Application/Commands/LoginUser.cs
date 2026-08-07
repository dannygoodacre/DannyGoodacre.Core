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

internal sealed partial class LoginUserHandler(ILogger<LoginUserHandler> logger,
                                               IStateUnit stateUnit,
                                               IUserRepository repository,
                                               IPasswordHashingService hashingService)
    : StateCommandHandler<LoginUserCommand, Guid>(logger, stateUnit), ILoginUser
{
    protected override string CommandName => "Login User";

    public new Task<Result<Guid>> ExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
        => base.ExecuteAsync(command, cancellationToken);

    protected override void Validate(ValidationState validationState, LoginUserCommand command)
    {
        validationState.IsNotNullEmptyOrWhitespace(command.Username, nameof(command.Username));

        validationState.IsNotNullEmptyOrWhitespace(command.Password, nameof(command.Password));
    }

    protected async override Task<Result<Guid>> InternalExecuteAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        LogStarted(Logger, CommandName, command.Username);

        var user = await repository.GetWithTrackingAsync(command.Username, cancellationToken);

        if (user is null)
        {
            LogNotFound(Logger, CommandName, command.Username);

            return NotFound();
        }

        if (!user.IsApproved)
        {
            LogUserNotApproved(Logger, CommandName, user.Username);

            return DomainError("User not approved");
        }

        if (!hashingService.Verify(command.Password, user.PasswordHash))
        {
            LogIncorrectPassword(Logger, CommandName, user.Username);

            return DomainError("Incorrect password");
        }

        user.SecurityStamp = Guid.NewGuid().ToString();

        user.LastLogin = DateTime.UtcNow;

        LogCompleted(Logger, CommandName, user.Username);

        return Success(user.PublicId);
    }

    [LoggerMessage(LogLevel.Information, "Command '{Command}' started for Username '{Username}'.")]
    private static partial void LogStarted(ILogger logger, string command, string username);

    [LoggerMessage(LogLevel.Warning, "Command '{Command}' failed: User with Username '{Username}' not found.")]
    private static partial void LogNotFound(ILogger logger, string command, string username);

    [LoggerMessage(LogLevel.Warning, "Command '{Command}' failed: User with Username '{Username}' not approved.")]
    private static partial void LogUserNotApproved(ILogger logger, string command, string username);

    [LoggerMessage(LogLevel.Warning, "Command '{Command}' failed: Incorrect password provided for Username '{Username}'.")]
    private static partial void LogIncorrectPassword(ILogger logger, string command, string username);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' completed for Username '{Username}'.")]
    private static partial void LogCompleted(ILogger logger, string command, string username);
}
