using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddClaim
{
    Task<Result<ClaimResponse>> ExecuteAsync(string type, string value, CancellationToken cancellationToken = default);
}

internal sealed record AddClaimCommand : ICommand
{
    public required string Type { get; init; }

    public required string Value { get; init; }
}

internal sealed class AddClaimHandler(ILogger<AddClaimHandler> logger,
                                      IStateUnit stateUnit,
                                      IClaimRepository repository)
    : StateCommandHandler<AddClaimCommand, ClaimResponse>(logger, stateUnit), IAddClaim
{
    protected override string CommandName => "Add Claim";

    protected async override Task<Result<ClaimResponse>> InternalExecuteAsync(AddClaimCommand command, CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsAsync(command.Type, command.Value, cancellationToken))
        {
            return Conflict("Claim already exists");
        }

        Claim claim = repository.Add(new Claim()
        {
            PublicId = Guid.NewGuid(),
            Type = command.Type,
            Value = command.Value
        });

        return Success(claim.ToResponse());
    }

    public Task<Result<ClaimResponse>> ExecuteAsync(string type, string value, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddClaimCommand
        {
            Type = type,
            Value = value
        }, cancellationToken);
}
