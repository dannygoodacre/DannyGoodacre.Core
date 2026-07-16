using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DannyGoodacre.Identity;

internal static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/users", async ([FromServices] IAddUser addUser,
                                           [FromBody] RegistrationRequest request,
                                           CancellationToken cancellationToken) =>
        {
            Result<UserInfo> result = await addUser.ExecuteAsync(request.Username, request.Password, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ToHttpResponse();
            }

            UserInfo userInfo = result.Value;

            return Results.CreatedAtRoute("GetUser", new { id = userInfo.Id }, userInfo);
        });

        endpoints.MapGet("/users/{id:guid}", async ([FromServices] IGetUser getUser,
                                                    [FromRoute] Guid id,
                                                    HttpContext httpContext,
                                                    CancellationToken cancellationToken) =>
        {
            if (!httpContext.IsSelfOrAdmin(id))
            {
                return Results.Forbid();
            }

            Result<UserInfo> result = await getUser.ExecuteAsync(id, cancellationToken);

            return result.ToHttpResponse();
        })
        .WithName("GetUser")
        .RequireAuthorization();

        endpoints.MapGet("/users/me", async ([FromServices] IGetUser getUser,
                                             HttpContext httpContext,
                                             CancellationToken cancellationToken) =>
        {
            Guid? id = httpContext.GetUserId();

            if (id is null)
            {
                return Results.Forbid();
            }

            Result<UserInfo> result = await getUser.ExecuteAsync(id.Value, cancellationToken);

            return result.ToHttpResponse();
        })
        .RequireAuthorization();

        endpoints.MapDelete("/users/{id:guid}", async ([FromServices] IDeleteUser deleteUser,
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
