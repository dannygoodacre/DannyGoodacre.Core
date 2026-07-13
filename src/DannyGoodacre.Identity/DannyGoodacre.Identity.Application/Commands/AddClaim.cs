using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddClaim
{
    Task<Result> ExecuteAsync(string type, string value, CancellationToken cancellationToken = default);
}

internal sealed record AddClaimCommand : ICommand
{
    public required string Type { get; init; }

    public required string Value { get; init; }
}

internal sealed class AddClaimHandler(ILogger<AddClaimHandler> logger, IStateUnit stateUnit, IClaimRepository repository)
    : StateCommandHandler<AddClaimCommand>(logger, stateUnit), IAddClaim
{
    protected override string CommandName => "Add Claim";

    protected override Task<Result> InternalExecuteAsync(AddClaimCommand command, CancellationToken cancellationToken = default)
    {
        _ = repository.Add(new Claim
        {
            Type = command.Type,
            Value = command.Value
        });

        return Task.FromResult(Success());
    }

    public Task<Result> ExecuteAsync(string type, string value, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddClaimCommand
        {
            Type = type,
            Value = value
        }, cancellationToken);
}
