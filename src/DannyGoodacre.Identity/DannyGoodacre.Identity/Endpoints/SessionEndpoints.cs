using System.Security.Claims;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Configuration;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Services;
using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity.Endpoints;

internal static class SessionEndpoints
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapSessionEndpoints()
        {
            Configuration.CookieOptions options = endpoints.ServiceProvider
                .GetRequiredService<IOptions<IdentityOptions>>()
                .Value.Cookie;

            RouteGroupBuilder group = endpoints.MapGroup("").WithTags("Identity: Session");

            group.MapPost(options.LoginPath, async Task<IResult> ([FromServices] ILoginUser loginUser,
                                                                  [FromServices] IGetUserSecurityProfile getUserSecurityProfile,
                                                                  [FromServices] IClaimService claimService,
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

                Result<UserSecurityProfileResponse> result = await getUserSecurityProfile.ExecuteAsync(request.Username, cancellationToken);

                if (!result.IsSuccess)
                {
                    return result.ToHttpResponse();
                }

                UserSecurityProfileResponse profileResponse = result.Value;

                ClaimsPrincipal claimsPrincipal = claimService.CreateClaimsPrincipal(profileResponse);

                await cookieService.IssueCookieAsync(httpContext, claimsPrincipal.Claims.ToList());

                return Results.NoContent();
            });

            group.MapGet($"{options.LoginPath}/me", (HttpContext httpContext) => Results.Ok(httpContext.SessionInfoResponse))
                .RequireAuthorization();

            group.MapDelete(options.LoginPath, async Task<IResult> ([FromServices] ICookieService cookieService,
                                                                    HttpContext httpContext) =>
            {
                await cookieService.RevokeCookieAsync(httpContext);

                return Results.NoContent();
            })
            .RequireAuthorization();

            return endpoints;
        }
    }
}
