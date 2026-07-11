using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface ICreateRole
{
    Task<Result> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}

internal sealed record CreateRoleCommand : ICommand
{
    public required string Name { get; init; }
}

internal sealed class CreateRoleHandler(ILogger<CreateRoleHandler> logger,
                                        IStateUnit stateUnit,
                                        IRoleRepository repository)
    : StateCommandHandler<CreateRoleCommand>(logger, stateUnit), ICreateRole
{

    protected override string CommandName => "Create Role";

    protected async override Task<Result> InternalExecuteAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsAsync(command.Name, cancellationToken))
        {
            return Result.DomainError("Role already exists");
        }

        repository.Add(command.Name);

        return Result.Success();
    }

    public Task<Result> ExecuteAsync(string name, CancellationToken cancellationToken = default)
        => ExecuteAsync(new CreateRoleCommand
        {
            Name = name
        }, cancellationToken);
}
