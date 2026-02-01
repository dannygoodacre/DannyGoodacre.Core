using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
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
                                        IUnitOfWork unitOfWork,
                                        IRoleRepository repository)
    : PersistenceCommandHandler<CreateRoleCommand>(logger, unitOfWork), ICreateRole
{

    protected override string CommandName => "Create Role";

    protected async override Task<Result> InternalExecuteAsync(CreateRoleCommand command,
                                                               CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsAsync(command.Name, cancellationToken))
        {
            return Result.Failed("Role already exists");
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
