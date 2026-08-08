using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Entities;
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

    public Task<Result> ExecuteAsync(List<ClaimDefinition> claims, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddClaimsCommand
        {
            Claims = claims
        }, cancellationToken);

    protected override void Validate(ValidationState validationState, AddClaimsCommand command)
    {
        validationState.IsNotNullOrEmpty(command.Claims, nameof(command.Claims));
    }

    protected async override Task<Result> InternalExecuteAsync(AddClaimsCommand command, CancellationToken cancellationToken = default)
    {
        List<Claim> existingClaims = await repository.GetExistingAsync(command.Claims, cancellationToken);

        var missingClaims = command.Claims
            .Where(x => !existingClaims.Any(y => x.Type == y.Type && x.Value == y.Value))
            .ToList();

        if (missingClaims.Count == 0)
        {
            return Success();
        }

        foreach (ClaimDefinition claim in missingClaims)
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
}
