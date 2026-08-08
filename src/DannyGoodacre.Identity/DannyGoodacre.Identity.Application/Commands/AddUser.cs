using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Services;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddUser
{
    Task<Result<UserInfoResponse>> ExecuteAsync(AddUserCommand command, CancellationToken cancellationToken = default);
}

public sealed record AddUserCommand : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed partial class AddUserHandler(ILogger<AddUserHandler> logger,
                                             IStateUnit stateUnit,
                                             IPasswordValidatorService passwordValidatorService,
                                             IUserRepository repository,
                                             IPasswordHashingService hashingService)
    : StateCommandHandler<AddUserCommand, UserInfoResponse>(logger, stateUnit), IAddUser
{
    protected override string CommandName => "Add User";

    public new Task<Result<UserInfoResponse>> ExecuteAsync(AddUserCommand command, CancellationToken cancellationToken = default)
        => base.ExecuteAsync(command, cancellationToken);

    protected override void Validate(ValidationState validationState, AddUserCommand command)
    {
        validationState.IsNotNullEmptyOrWhitespace(command.Username, nameof(command.Username));

        passwordValidatorService.IsPasswordValid(validationState, command.Password);
    }

    protected async override Task<Result<UserInfoResponse>> InternalExecuteAsync(AddUserCommand command, CancellationToken cancellationToken = default)
    {
        LogStarted(Logger, CommandName, command.Username);

        if (await repository.ExistsAsync(command.Username, cancellationToken))
        {
            return Conflict("Username already taken");
        }

        string passwordHash = hashingService.Hash(command.Password);

        User user = repository.Add(User.CreateNew(command.Username, passwordHash));

        LogCompleted(Logger, CommandName, command.Username);

        return Success(user.ToUserInfoResponse());
    }

    [LoggerMessage(LogLevel.Information, "Command '{Command}' started for Username '{Username}'.")]
    private static partial void LogStarted(ILogger logger, string command, string username);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' completed for Username '{Username}'.")]
    private static partial void LogCompleted(ILogger logger, string command, string username);
}
