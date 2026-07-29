using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using TestProject.Repositories;

namespace TestProject;

internal interface IAddClaim
{
    Task<Result> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}

internal sealed record AddClaimCommand : ICommand
{
    public required string Name { get; init; }
}

internal sealed class AddClaimHandler(ILogger<AddClaimHandler> logger, IStateUnit stateUnit, IClaimRepository repository)
    : StateCommandHandler<AddClaimCommand>(logger, stateUnit), IAddClaim
{
    protected override string CommandName => "Add Claim";

    protected override void Validate(ValidationState validationState, AddClaimCommand command)
    {

    }

    protected override Task<Result> InternalExecuteAsync(AddClaimCommand command, CancellationToken cancellationToken = default)
    {
        repository.Add(new Claim
        {
            Name = command.Name
        });

        throw new Exception("Test");

        // return Task.FromResult(Success());
    }

    public Task<Result> ExecuteAsync(string name, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddClaimCommand
        {
            Name = name
        }, cancellationToken);
}
