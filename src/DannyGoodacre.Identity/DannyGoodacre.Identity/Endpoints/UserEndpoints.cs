using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DannyGoodacre.Identity.Endpoints;

internal static class UserEndpoints
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapUserEndpoints()
        {
            var group = endpoints
                .MapGroup("users")
                .WithTags("Identity: Users");

            group.MapPost("", async ([FromServices] IAddUser addUser,
                                     [FromBody] RegistrationRequest request,
                                     CancellationToken cancellationToken) =>
            {
                Result<UserInfoResponse> result = await addUser.ExecuteAsync(request.Username, request.Password, cancellationToken);

                if (!result.IsSuccess)
                {
                    return result.ToHttpResponse();
                }

                UserInfoResponse userInfoResponse = result.Value;

                return Results.CreatedAtRoute("GetUser", new { id = userInfoResponse.Id }, userInfoResponse);
            });

            group.MapPut("{id:guid}/approval", async ([FromServices] IApproveUser approveUser,
                                                      Guid id,
                                                      CancellationToken cancellationToken) =>
            {
                Result result = await approveUser.ExecuteAsync(id, cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization();

            group.MapGet("{id:guid}", async ([FromServices] IGetUser getUser,
                                             [FromRoute] Guid id,
                                             HttpContext httpContext,
                                             CancellationToken cancellationToken) =>
            {
                if (!httpContext.IsSelfOrAdmin(id))
                {
                    return Results.Forbid();
                }

                Result<UserInfoResponse> result = await getUser.ExecuteAsync(id, cancellationToken);

                return result.ToHttpResponse();
            })
            .WithName("GetUser")
            .RequireAuthorization();

            group.MapGet("me", async ([FromServices] IGetUser getUser,
                                      HttpContext httpContext,
                                      CancellationToken cancellationToken) =>
            {
                Guid? id = httpContext.UserId;

                if (id is null)
                {
                    return Results.Forbid();
                }

                Result<UserInfoResponse> result = await getUser.ExecuteAsync(id.Value, cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization();

            group.MapDelete("{id:guid}", async ([FromServices] IDeleteUser deleteUser,
                                                [FromRoute] Guid id,
                                                HttpContext httpContext,
                                                CancellationToken cancellationToken) =>
            {
                if (!httpContext.IsSelfOrAdmin(id))
                {
                    return Results.Forbid();
                }

                Result result = await deleteUser.ExecuteAsync(id, cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization();

            return endpoints;
        }
    }
}
