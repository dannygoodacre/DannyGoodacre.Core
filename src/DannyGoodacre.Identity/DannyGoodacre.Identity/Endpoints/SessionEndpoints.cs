using System.Security.Claims;
using DannyGoodacre.Identity.Application.Abstractions.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Configuration;
using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Claim = DannyGoodacre.Identity.Entities.Claim;

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

            group.MapPost(options.LoginPath, async Task<IResult> (IClaimService claimService,
                                                                  ICookieService cookieService,
                                                                  IPasswordHashingService hashingService,
                                                                  IUserRepository userRepository,
                                                                  IUserClaimRepository userClaimRepository,
                                                                  LoginRequest request,
                                                                  HttpContext httpContext,
                                                                  CancellationToken cancellationToken) =>
            {
                User? user = await userRepository.GetWithTrackingAsync(request.Username, cancellationToken);

                if (user is null || !hashingService.Verify(request.Password, user.PasswordHash))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Unauthorized",
                        detail: "Invalid username or password");
                }

                if (!user.IsApproved)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Forbidden",
                        detail: "User has not been approved yet");
                }

                List<Claim> userClaims = await userClaimRepository.GetManyAsync(user.Id, cancellationToken);

                List<ClaimDefinition> claims = userClaims.Select(x => new ClaimDefinition
                {
                    Type = x.Type,
                    Value = x.Value
                }).ToList();

                List<string> roles = user.Roles.Select(x => x.Role.Name).ToList();

                var profileResponse = new UserSecurityProfileResponse
                {
                    Id = user.PublicId,
                    Username = user.Username,
                    Claims = claims,
                    Roles = roles
                };

                ClaimsPrincipal claimsPrincipal = claimService.CreateClaimsPrincipal(profileResponse);

                await cookieService.IssueCookieAsync(httpContext, claimsPrincipal.Claims.ToList());

                return Results.NoContent();
            });

            group.MapGet($"{options.LoginPath}/me", (HttpContext httpContext) => Results.Ok(httpContext.SessionInfoResponse))
                .RequireAuthorization();

            group.MapDelete(options.LoginPath, async Task<IResult> (ICookieService cookieService,
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
