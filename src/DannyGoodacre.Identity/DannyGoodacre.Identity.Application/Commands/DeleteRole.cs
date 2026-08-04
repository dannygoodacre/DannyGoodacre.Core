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

internal sealed class DeleteRoleHandler(ILogger<DeleteRoleHandler> logger,
                                        IStateUnit stateUnit,
                                        IRoleRepository repository)
    : StateCommandHandler<DeleteRoleCommand>(logger, stateUnit), IDeleteRole
{

    protected override string CommandName => "Delete Role";

    protected async override Task<Result> InternalExecuteAsync(DeleteRoleCommand command, CancellationToken cancellationToken = default)
    {
        Role? role = await repository.GetAsync(command.Id, cancellationToken);

        if (role is null)
        {
            return NotFound();
        }

        repository.Remove(role);

        return Success();
    }

    public Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteAsync(new DeleteRoleCommand()
        {
            Id = id
        }, cancellationToken);
}
