using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddClaims
{
    Task<Result> ExecuteAsync(List<ClaimDefinition> claims, CancellationToken cancellationToken = default);
}

internal sealed record AddClaimsCommand : ICommand
{
    public required List<ClaimDefinition> Claims { get; init; }
}

internal sealed class AddClaimsHandler(ILogger<AddClaimsHandler> logger,
                                       IStateUnit stateUnit,
                                       IClaimRepository repository)
    : StateCommandHandler<AddClaimsCommand>(logger, stateUnit), IAddClaims
{
    protected override string CommandName => "Add Claims";

    protected async override Task<Result> InternalExecuteAsync(AddClaimsCommand command, CancellationToken cancellationToken = default)
    {
        List<ClaimDefinition> alreadyExistingClaims = [];

        foreach (ClaimDefinition claim in command.Claims)
        {
            if (await repository.ExistsAsync(claim.Type, claim.Value, cancellationToken))
            {
                alreadyExistingClaims.Add(claim);
            }
        }

        if (alreadyExistingClaims.Count > 0)
        {
            string claims = string.Join(", ", alreadyExistingClaims.Select(c => $"'{c.Type}: {c.Value}'"));

            return Conflict($"The following claims already exist: {claims}.");
        }

        foreach (ClaimDefinition claim in command.Claims)
        {
            _ = repository.Add(new Claim
            {
                PublicId = Guid.NewGuid(),
                Type = claim.Type,
                Value = claim.Value
            });
        }

        return Success();
    }

    public Task<Result> ExecuteAsync(List<ClaimDefinition> claims, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddClaimsCommand
        {
            Claims = claims
        }, cancellationToken);
}
