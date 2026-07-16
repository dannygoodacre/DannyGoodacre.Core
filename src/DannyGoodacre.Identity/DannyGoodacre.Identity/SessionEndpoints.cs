using System.Security.Claims;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Services;
using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DannyGoodacre.Identity;

internal static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/session", async Task<IResult> ([FromServices] ILoginUser loginUser,
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

            ClaimsPrincipal claimsPrincipal = claimsService.Create(profile);

            await cookieService.IssueCookieAsync(httpContext, claimsPrincipal.Claims.ToList());

            return Results.NoContent();
        });

        endpoints.MapGet("/session", (HttpContext httpContext) =>
        {
            var sessionInfo = new
            {
                IsAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false,
                Username = httpContext.User.Identity?.Name,
                Claims = httpContext.User.Claims.Select(c => new { c.Type, c.Value })
            };

            return Results.Ok(sessionInfo);
        })
        .RequireAuthorization();

        endpoints.MapDelete("/session", async Task<IResult> ([FromServices] ICookieService cookieService,
                                                             HttpContext httpContext) =>
        {
            await cookieService.RevokeCookieAsync(httpContext);

            return Results.NoContent();
        })
        .RequireAuthorization();

        return endpoints;
    }
}
