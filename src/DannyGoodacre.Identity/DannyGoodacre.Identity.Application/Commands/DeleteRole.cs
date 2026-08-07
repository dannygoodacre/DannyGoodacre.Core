using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IDeleteRole
{
    Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed record DeleteRoleCommand : ICommand
{
    public required Guid Id { get; init; }
}

internal sealed partial class DeleteRoleHandler(ILogger<DeleteRoleHandler> logger,
                                        IStateUnit stateUnit,
                                        IRoleRepository repository)
    : StateCommandHandler<DeleteRoleCommand>(logger, stateUnit), IDeleteRole
{

    protected override string CommandName => "Delete Role";

    protected override void Validate(ValidationState validationState, DeleteRoleCommand command)
    {
        validationState.IsNonEmptyGuid(command.Id, nameof(command.Id));
    }

    protected async override Task<Result> InternalExecuteAsync(DeleteRoleCommand command, CancellationToken cancellationToken = default)
    {
        LogStarted(Logger, CommandName, command.Id);

        Role? role = await repository.GetAsync(command.Id, cancellationToken);

        if (role is null)
        {
            LogNotFound(Logger, CommandName, command.Id);

            return NotFound();
        }

        repository.Remove(role);

        LogCompleted(Logger, CommandName, command.Id);

        return Success();
    }

    public Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteAsync(new DeleteRoleCommand
        {
            Id = id
        }, cancellationToken);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' started for Role ID '{RoleId}'.")]
    private static partial void LogStarted(ILogger logger, string command, Guid roleId);

    [LoggerMessage(LogLevel.Warning, "Command '{Command}' failed: Role ID '{RoleId}' not found.")]
    private static partial void LogNotFound(ILogger logger, string command, Guid roleId);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' completed for Role ID '{RoleId}'.")]
    private static partial void LogCompleted(ILogger logger, string command, Guid roleId);
}
