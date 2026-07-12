using System.Security.Claims;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Services;
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

            group.MapPost("/users", async Task<IResult> ([FromServices] ICreateUser createUser,
                                                         [FromBody] RegistrationRequest request,
                                                          CancellationToken cancellationToken) =>
            {
                var result = await createUser.ExecuteAsync(request.Username, request.Password, cancellationToken);

                return result.ToHttpResponse();
            });

            group.MapPost("/session", async Task<IResult> ([FromServices] ILoginUser loginUser,
                                                           [FromServices] ICookieService cookieService,
                                                           [FromBody] LoginRequest request,
                                                           HttpContext httpContext,
                                                           CancellationToken cancellationToken) =>
            {
                var result = await loginUser.ExecuteAsync(request.Username, request.Password, cancellationToken);

                if (!result.IsSuccess)
                {
                    return result.ToHttpResponse();
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, request.Username),
                    new("SecurityStamp", result.Value)
                };

                await cookieService.IssueCookieAsync(httpContext, claims);

                return Results.NoContent();
            });

            group.MapDelete("/session", async Task<IResult> ([FromServices] ICookieService cookieService,
                                                             HttpContext httpContext) =>
            {
                await cookieService.RevokeCookieAsync(httpContext);

                return Results.NoContent();
            })
            .RequireAuthorization();

            return group;
        }
    }
}
