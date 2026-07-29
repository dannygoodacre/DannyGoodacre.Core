using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using TestProject.Repositories;

namespace TestProject;

internal interface IAddUser
{
    Task<Result> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}

internal sealed record AddUserCommand : ICommand
{
    public required string Name { get; init; }
}

internal sealed class AddUserHandler(ILogger<AddUserHandler> logger, IStateUnit stateUnit, IUserRepository repository)
    : StateCommandHandler<AddUserCommand>(logger, stateUnit), IAddUser
{
    protected override string CommandName => "Add User";

    protected override Task<Result> InternalExecuteAsync(AddUserCommand command, CancellationToken cancellationToken = default)
    {
        repository.Add(new User
        {
            Name = command.Name
        });

        return Task.FromResult(Success());
    }

    public Task<Result> ExecuteAsync(string name, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddUserCommand
        {
            Name = name
        }, cancellationToken);
}
