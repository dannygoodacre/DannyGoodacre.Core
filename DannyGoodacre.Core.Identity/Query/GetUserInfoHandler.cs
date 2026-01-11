using System.Security.Claims;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Core.Identity.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Identity.Query;

public sealed class GetUserInfoHandler(ILogger<GetUserInfoHandler> logger,
                                       IHttpContextAccessor httpContextAccessor,
                                       UserManager<ApplicationUser> userManager)
    : QueryHandler<GetUserInfoRequest, UserInfoResponse>(logger), IGetUserInfo
{

    protected override string QueryName => "Get User Info";

    protected async override Task<Result<UserInfoResponse>> InternalExecuteAsync(GetUserInfoRequest query, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity is null || !httpContext.User.Identity.IsAuthenticated)
        {
            return Result<UserInfoResponse>.NotFound();
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Result<UserInfoResponse>.NotFound();
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result<UserInfoResponse>.NotFound();
        }

        return Result.Success(new UserInfoResponse
        {
            Username = user.UserName!,
            IsAccountConfirmed = user.EmailConfirmed
        });
    }

    public Task<Result<UserInfoResponse>> ExecuteAsync(CancellationToken cancellationToken)
        => ExecuteAsync(new GetUserInfoRequest(), cancellationToken);
}

public sealed record GetUserInfoRequest : IQuery;

public interface IGetUserInfo
{
    Task<Result<UserInfoResponse>> ExecuteAsync(CancellationToken cancellationToken = default);
}
