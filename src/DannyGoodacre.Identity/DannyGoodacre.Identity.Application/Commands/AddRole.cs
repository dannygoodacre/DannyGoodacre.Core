using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddRole
{
    Task<Result> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}

internal sealed record AddRoleCommand : ICommand
{
    public required string Name { get; init; }
}

internal sealed class AddRoleHandler(ILogger<AddRoleHandler> logger,
                                        IStateUnit stateUnit,
                                        IRoleRepository repository)
    : StateCommandHandler<AddRoleCommand>(logger, stateUnit), IAddRole
{

    protected override string CommandName => "Add Role";

    protected async override Task<Result> InternalExecuteAsync(AddRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsAsync(command.Name, cancellationToken))
        {
            return DomainError("Role already exists");
        }

        repository.Add(command.Name);

        return Success();
    }

    public Task<Result> ExecuteAsync(string name, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddRoleCommand
        {
            Name = name
        }, cancellationToken);
}
