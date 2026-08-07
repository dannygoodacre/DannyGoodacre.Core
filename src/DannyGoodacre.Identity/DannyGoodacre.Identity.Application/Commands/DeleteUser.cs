using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IDeleteUser
{
    Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed record DeleteUserCommand : ICommand
{
    public required Guid Id { get; init; }
}

internal sealed partial class DeleteUserHandler(ILogger<DeleteUserHandler> logger,
                                                IStateUnit stateUnit,
                                                IUserRepository repository)
    : StateCommandHandler<DeleteUserCommand>(logger, stateUnit), IDeleteUser
{
    protected override string CommandName => "Delete User";

    public Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteAsync(new DeleteUserCommand
        {
            Id = id
        }, cancellationToken);

    protected override void Validate(ValidationState validationState, DeleteUserCommand command)
    {
        validationState.IsNonEmptyGuid(command.Id, nameof(command.Id));
    }

    protected async override Task<Result> InternalExecuteAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        LogStarted(Logger, CommandName, command.Id);

        User? user = await repository.GetAsync(command.Id, cancellationToken);

        if (user is null)
        {
            LogNotFound(Logger, CommandName, command.Id);

            return NotFound();
        }

        repository.Remove(user);

        LogCompleted(Logger, CommandName, command.Id);

        return Success();
    }

    [LoggerMessage(LogLevel.Information, "Command '{Command}' started for User ID '{UserId}'.")]
    private static partial void LogStarted(ILogger logger, string command, Guid userId);

    [LoggerMessage(LogLevel.Warning, "Command '{Command}' failed: User ID '{UserId}' not found.")]
    private static partial void LogNotFound(ILogger logger, string command, Guid userId);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' completed for User ID '{UserId}'.")]
    private static partial void LogCompleted(ILogger logger, string command, Guid userId);
}
