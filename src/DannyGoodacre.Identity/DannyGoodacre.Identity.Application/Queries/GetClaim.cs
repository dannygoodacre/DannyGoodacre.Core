using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Queries;

public interface IGetClaim
{
    Task<Result<ClaimResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed record GetClaimQuery : IQuery
{
    public required Guid Id { get; init; }
}

internal sealed class GetClaimHandler(ILogger<GetClaimHandler> logger, IClaimRepository repository)
    : QueryHandler<GetClaimQuery, ClaimResponse>(logger), IGetClaim
{
    protected override string QueryName => "Get Claim";

    protected override void Validate(ValidationState validationState, GetClaimQuery query)
    {
        validationState.IsNonEmptyGuid(query.Id, nameof(query.Id));
    }

    protected async override Task<Result<ClaimResponse>> InternalExecuteAsync(GetClaimQuery query, CancellationToken cancellationToken = default)
    {
        Claim? claim = await repository.GetAsync(query.Id, cancellationToken);

        return claim is null
            ? NotFound()
            : Success(claim.ToResponse());
    }

    public Task<Result<ClaimResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteAsync(new GetClaimQuery
        {
            Id = id
        }, cancellationToken);
}
