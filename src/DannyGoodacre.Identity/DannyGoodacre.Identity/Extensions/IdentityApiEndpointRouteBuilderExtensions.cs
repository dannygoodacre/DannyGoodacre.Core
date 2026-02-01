using System.Security.Claims;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Extensions;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

// ReSharper disable once CheckNamespace
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

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToUnsuccessfulHttpResponse();
            });

            group.MapPost("/session", async Task<IResult> ([FromServices] ILoginUser loginUser,
                                                           [FromServices] ICookieService cookieService,
                                                           [FromBody] LoginRequest request,
                                                           CancellationToken cancellationToken) =>
            {
                var result = await loginUser.ExecuteAsync(request.Username, request.Password, cancellationToken);

                if (!result.IsSuccess)
                {
                    return result.ToUnsuccessfulHttpResponse();
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, request.Username),
                    new("SecurityStamp", result.Value)
                };

                await cookieService.IssueCookie(claims);

                return Results.NoContent();
            });

            return group;
        }
    }
}
