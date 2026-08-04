using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Queries;

public interface IGetUserSecurityProfile
{
    Task<Result<UserSecurityProfile>> ExecuteAsync(string username, CancellationToken cancellationToken = default);
}

internal sealed record GetUserSecurityProfileQuery : IQuery
{
    public required string Username { get; init; }
}

internal sealed class GetUserSecurityProfileHandler(ILogger<GetUserSecurityProfileHandler> logger,
                                                    IUserRepository userRepository,
                                                    IUserClaimRepository userClaimRepository)
    : QueryHandler<GetUserSecurityProfileQuery, UserSecurityProfile>(logger), IGetUserSecurityProfile
{

    protected override string QueryName => "Get User Security Profile";

    protected async override Task<Result<UserSecurityProfile>> InternalExecuteAsync(GetUserSecurityProfileQuery query, CancellationToken cancellationToken = default)
    {
        User? user = await userRepository.GetAsync(query.Username, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        List<Claim> userClaims = await userClaimRepository.GetManyAsync(user.Id, cancellationToken);

        List<ClaimDefinition> claims = userClaims.Select(x => new ClaimDefinition
        {
            Type = x.Type,
            Value = x.Value
        }).ToList();

        List<string> roles = user.Roles.Select(x => x.Role.Name).ToList();

        return Success(new UserSecurityProfile
        {
            Id = user.PublicId,
            Username = user.Username,
            SecurityStamp = user.SecurityStamp,
            Claims = claims,
            Roles = roles
        });
    }

    public Task<Result<UserSecurityProfile>> ExecuteAsync(string username, CancellationToken cancellationToken = default)
        => ExecuteAsync(new GetUserSecurityProfileQuery
        {
            Username = username
        }, cancellationToken);
}
