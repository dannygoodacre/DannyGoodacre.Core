using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Models;
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

            group.MapPost("/users", async Task<IResult> (
                [FromServices] IRegisterNewUser registerNewUser,
                [FromBody] RegistrationRequest registrationRequest,
                CancellationToken cancellationToken) =>
            {
                var result = await registerNewUser.ExecuteAsync(registrationRequest.Username,
                                                                registrationRequest.Password,
                                                                cancellationToken);

                // return result.Status switch
                // {
                //
                //     Status.Success => Results.Created($"/users/{result.Value?.Id}", result.Value),
                //     Status.Invalid => Results.ValidationProblem(result.ValidationState!.Errors.ToDictionary(x => x.Key, x => x.Value.ToArray())),
                //     Status.DomainError => Results.ValidationProblem(result.ValidationState!.Errors.ToDictionary(x => x.Key, x => x.Value.ToArray())),
                //     Status.Cancelled => Results.StatusCode(499),
                //     Status.NotFound => Results.NotFound(),
                //     _ => Results.InternalServerError()
                // };

                return result.IsSuccess
                    ? Results.Created($"/users/{result.Value?.Id}", result.Value)
                    : result.ToHttpResponse();
            });

            group.MapGet("/users/{userId}", async Task<IResult> (
                [FromServices] IGetUserInfo getUserInfo,
                string userId,
                CancellationToken cancellationToken) =>
            {
                var result = await getUserInfo.ExecuteAsync(userId, cancellationToken);

                return result.ToHttpResponse();
            });

            group.MapPost("/users/{userId}/approval", async Task<IResult> (
                [FromServices] IApproveUser approveUser,
                string userId,
                CancellationToken cancellationToken) =>
            {
                var result = await approveUser.ExecuteAsync(userId, cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization(x => x.RequireRole("Admin"));

            group.MapPost("/users/me/password", async Task<IResult> (
                [FromServices] IChangePassword changePassword,
                [FromBody] ChangePasswordRequest changePasswordRequest,
                CancellationToken cancellationToken) =>
            {
                var result = await changePassword.ExecuteAsync(changePasswordRequest.OldPassword,
                                                               changePasswordRequest.NewPassword,
                                                               cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization();

            group.MapPost("/session", async Task<IResult> (
                [FromServices] ILogin login,
                [FromBody] LoginRequest loginRequest,
                CancellationToken cancellationToken) =>
            {
                var result = await login.ExecuteAsync(loginRequest.Username,
                                                      loginRequest.Password,
                                                      cancellationToken);

                return result.ToHttpResponse();
            });

            group.MapDelete("/session", async Task<IResult> (
                [FromServices] ILogout logout,
                CancellationToken cancellationToken) =>
            {
                var result = await logout.ExecuteAsync(cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization();

            return group;
        }
    }
}
