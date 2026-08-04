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

internal sealed class DeleteUserHandler(ILogger<DeleteUserHandler> logger,
                                        IStateUnit stateUnit,
                                        IUserRepository repository)
    : StateCommandHandler<DeleteUserCommand>(logger, stateUnit), IDeleteUser
{

    protected override string CommandName => "Delete User";

    protected async override Task<Result> InternalExecuteAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        User? user = await repository.GetAsync(command.Id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        repository.Remove(user);

        return Success();
    }

    public Task<Result> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteAsync(new DeleteUserCommand()
        {
            Id = id
        }, cancellationToken);
}
