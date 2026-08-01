using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Queries;

public interface IGetAllClaims
{
    Task<Result<List<ClaimResponse>>> ExecuteAsync(CancellationToken cancellationToken = default);
}

internal sealed record GetAllClaimsQuery : IQuery;

internal sealed class GetAllClaimsHandler(ILogger<GetAllClaimsHandler> logger, IClaimRepository repository)
    : QueryHandler<GetAllClaimsQuery, List<ClaimResponse>>(logger), IGetAllClaims
{
    protected override string QueryName => "Get All Claims";

    protected async override Task<Result<List<ClaimResponse>>> InternalExecuteAsync(GetAllClaimsQuery query, CancellationToken cancellationToken = default)
    {
        List<Claim> claims = await repository.GetAllAsync(cancellationToken);

        List<ClaimResponse> claimResponses = claims.Select(x => x.ToResponse()).ToList();

        return Success(claimResponses);
    }

    public Task<Result<List<ClaimResponse>>> ExecuteAsync(CancellationToken cancellationToken = default)
        => base.ExecuteAsync(new GetAllClaimsQuery(), cancellationToken);
}
