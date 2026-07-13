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

public static class IdentityApiEndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointConventionBuilder MapIdentityEndpoints()
        {
            var group = endpoints.MapGroup("").WithTags("Identity");

            group.MapPost("/users", async Task<IResult> ([FromServices] IAddUser addUser,
                                                         [FromBody] RegistrationRequest request,
                                                          CancellationToken cancellationToken) =>
            {
                var result = await addUser.ExecuteAsync(request.Username, request.Password, cancellationToken);

                return result.ToHttpResponse();
            });

            group.MapPost("/session", async Task<IResult> ([FromServices] ILoginUser loginUser,
                                                           [FromServices] IGetUserSecurityProfile getUserSecurityProfile,
                                                           [FromServices] IClaimsService claimsService,
                                                           [FromServices] ICookieService cookieService,
                                                           [FromBody] LoginRequest request,
                                                           HttpContext httpContext,
                                                           CancellationToken cancellationToken) =>
            {
                Result<int> loginResult = await loginUser.ExecuteAsync(request.Username, request.Password, cancellationToken);

                if (!loginResult.IsSuccess)
                {
                    return loginResult.ToHttpResponse();
                }

                Result<UserSecurityProfile> result = await getUserSecurityProfile.ExecuteAsync(request.Username , cancellationToken);

                if (!result.IsSuccess)
                {
                    return result.ToHttpResponse();
                }

                UserSecurityProfile profile = result.Value;

                ClaimsPrincipal claimsPrincipal = claimsService.Create(profile);

                await cookieService.IssueCookieAsync(httpContext, claimsPrincipal.Claims.ToList());

                return Results.NoContent();
            });

            group.MapDelete("/session", async Task<IResult> ([FromServices] ICookieService cookieService,
                                                             HttpContext httpContext) =>
            {
                await cookieService.RevokeCookieAsync(httpContext);

                return Results.NoContent();
            })
            .RequireAuthorization();

            group.MapPost("claim", async Task<IResult> ([FromServices] IAddClaim addClaim,
                                                        [FromBody] CreateClaimRequest request,
                                                        CancellationToken cancellationToken) =>
            {
                Result result = await addClaim.ExecuteAsync(request.Type, request.Value, cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization();

            group.MapPost("role", async Task<IResult> ([FromServices] IAddRole createRole,
                                                       [FromBody] string roleName,
                                                       CancellationToken cancellationToken) =>
            {
                Result result = await createRole.ExecuteAsync(roleName, cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization();

            return group;
        }
    }
}
