using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DannyGoodacre.Identity;

using LoginRequest = Model.LoginRequest;

public static class IdentityApiEndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointConventionBuilder MapIdentityEndpoints()
        {
            var group = endpoints.MapGroup("/auth").WithTags("Identity");

            group.MapPost("/register", async Task<IResult> (
                [FromServices] IServiceProvider serviceProvider,
                [FromBody] RegistrationRequest registrationRequest,
                CancellationToken cancellationToken) =>
            {
                var register = serviceProvider.GetRequiredService<IRegisterNewUser>();

                var result = await register.ExecuteAsync(registrationRequest.Username,
                                                         registrationRequest.Password,
                                                         cancellationToken);

                return result.ToHttpResponse();
            });

            group.MapPost("/login", async Task<IResult> (
                [FromServices] IServiceProvider serviceProvider,
                [FromBody] LoginRequest loginRequest,
                CancellationToken cancellationToken) =>
            {
                var login = serviceProvider.GetRequiredService<ILogin>();

                var result = await login.ExecuteAsync(loginRequest.Username,
                                                      loginRequest.Password,
                                                      cancellationToken);

                return result.ToHttpResponse();
            });

            group.MapPost("/logout", async Task<IResult> (
                [FromServices] IServiceProvider serviceProvider,
                CancellationToken cancellationToken) =>
            {
                var logout = serviceProvider.GetRequiredService<ILogout>();

                var result = await logout.ExecuteAsync(cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization();

            group.MapPost("changepassword", async Task<IResult> (
                [FromServices] IServiceProvider serviceProvider,
                [FromBody] ChangePasswordRequest changePasswordRequest,
                CancellationToken cancellationToken) =>
            {
                var changePassword = serviceProvider.GetRequiredService<IChangePassword>();

                var result = await changePassword.ExecuteAsync(changePasswordRequest.OldPassword,
                                                               changePasswordRequest.NewPassword,
                                                               cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization();

            group.MapPost("/{userId}/approve", async Task<IResult> (
                string userId,
                [FromServices] IServiceProvider serviceProvider,
                CancellationToken cancellationToken) =>
            {
                var approveUser = serviceProvider.GetRequiredService<IApproveUser>();

                var result = await approveUser.ExecuteAsync(userId, cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            return group;
        }
    }
}
