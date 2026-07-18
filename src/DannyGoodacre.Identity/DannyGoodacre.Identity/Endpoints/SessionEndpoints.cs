using System.Security.Claims;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Services;
using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DannyGoodacre.Identity.Endpoints;

internal static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("sessions").WithTags("Identity: Session");

        group.MapPost("", async Task<IResult> ([FromServices] ILoginUser loginUser,
                                               [FromServices] IGetUserSecurityProfile getUserSecurityProfile,
                                               [FromServices] IClaimsService claimsService,
                                               [FromServices] ICookieService cookieService,
                                               [FromBody] LoginRequest request,
                                               HttpContext httpContext,
                                               CancellationToken cancellationToken) =>
        {
            Result<Guid> loginResult = await loginUser.ExecuteAsync(request.ToCommand(), cancellationToken);

            if (!loginResult.IsSuccess)
            {
                return loginResult.ToHttpResponse();
            }

            Result<UserSecurityProfile> result = await getUserSecurityProfile.ExecuteAsync(request.Username, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ToHttpResponse();
            }

            UserSecurityProfile profile = result.Value;

            ClaimsPrincipal claimsPrincipal = claimsService.CreateClaimsPrincipal(profile);

            await cookieService.IssueCookieAsync(httpContext, claimsPrincipal.Claims.ToList());

            return Results.NoContent();
        });

        group.MapGet("me", (HttpContext httpContext) => Results.Ok(httpContext.SessionInfo))
            .RequireAuthorization();

        group.MapDelete("me", async Task<IResult> ([FromServices] ICookieService cookieService,
                                                   HttpContext httpContext) =>
        {
            await cookieService.RevokeCookieAsync(httpContext);

            return Results.NoContent();
        })
        .RequireAuthorization("Permission:Users.Logout");
        // .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        return endpoints;
    }
}
