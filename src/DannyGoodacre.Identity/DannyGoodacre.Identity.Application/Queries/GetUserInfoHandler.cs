using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Core;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Queries;

public interface IGetUserInfo
{
    Task<Result<UserInfoResponse>> ExecuteAsync(string userId, CancellationToken cancellationToken = default);
}

internal sealed record GetUserInfoQuery : IQuery
{
    public required string UserId { get; init; }
}

internal sealed class GetUserInfoHandler(ILogger<GetUserInfoHandler> logger,
                                         IUserManager<IdentityUser> userManager)
    : QueryHandler<GetUserInfoQuery, UserInfoResponse>(logger), IGetUserInfo
{

    protected override string QueryName => "Get User Information";

    protected override void Validate(ValidationState validationState, GetUserInfoQuery query)
        => validationState.IsNotNullEmptyOrWhitespace(query.UserId, nameof(query.UserId));

    protected async override Task<Result<UserInfoResponse>> InternalExecuteAsync(GetUserInfoQuery query, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(query.UserId);

        return user is null
            ? Result.NotFound()
            : Result.Success(user.ToUserInfoResponse());
    }

    public Task<Result<UserInfoResponse>> ExecuteAsync(string userId, CancellationToken cancellationToken)
        => ExecuteAsync(new GetUserInfoQuery
        {
            UserId = userId
        }, cancellationToken);
}
