using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Repositories;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Identity.Models;
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

            group.MapPost("", async (IPasswordHashingService hashingService,
                                     IUserRepository repository,
                                     IStateUnit stateUnit,
                                     RegistrationRequest request,
                                     CancellationToken cancellationToken) =>
            {
                if (await repository.ExistsAsync(request.Username, cancellationToken))
                {
                    return Results.Conflict("Username already taken");
                }

                string passwordHash = hashingService.Hash(request.Password);

                User user = repository.Add(User.CreateNew(request.Username, passwordHash));

                await stateUnit.SaveChangesAsync(cancellationToken);

                return Results.CreatedAtRoute("GetUser", new { id = user.PublicId }, user.ToUserInfoResponse());
            });

            group.MapPut("{id:guid}/approval", async (IUserRepository repository,
                                                      IStateUnit stateUnit,
                                                      Guid id,
                                                      CancellationToken cancellationToken) =>
            {
                User? user = await repository.GetWithTrackingAsync(id, cancellationToken);

                if (user is null)
                {
                    return Results.NotFound();
                }

                user.IsApproved = true;

                await stateUnit.SaveChangesAsync(cancellationToken);

                return Results.NoContent();
            })
            .RequireAuthorization();

            group.MapGet("{id:guid}", async (IUserRepository repository,
                                             [FromRoute] Guid id,
                                             HttpContext httpContext,
                                             CancellationToken cancellationToken) =>
            {
                if (!httpContext.IsSelfOrAdmin(id))
                {
                    return Results.Forbid();
                }

                User? user = await repository.GetAsync(id, cancellationToken);

                return user is null
                    ? Results.NotFound()
                    : Results.Ok(user.ToUserInfoResponse());
            })
            .WithName("GetUser")
            .RequireAuthorization();

            group.MapGet("me", async (IUserRepository repository,
                                      HttpContext httpContext,
                                      CancellationToken cancellationToken) =>
            {
                Guid? id = httpContext.UserId;

                if (id is null)
                {
                    return Results.Forbid();
                }

                User? user = await repository.GetAsync(id.Value, cancellationToken);

                return user is null
                    ? Results.NotFound()
                    : Results.Ok(user.ToUserInfoResponse());
            })
            .RequireAuthorization();

            group.MapDelete("{id:guid}", async (IUserRepository repository,
                                                IStateUnit stateUnit,
                                                [FromRoute] Guid id,
                                                HttpContext httpContext,
                                                CancellationToken cancellationToken) =>
            {
                if (!httpContext.IsSelfOrAdmin(id))
                {
                    return Results.Forbid();
                }

                User? user = await repository.GetAsync(id, cancellationToken);

                if (user is null)
                {
                    return Results.NotFound();
                }

                repository.Remove(user);

                await stateUnit.SaveChangesAsync(cancellationToken);

                return Results.NoContent();
            })
            .RequireAuthorization();

            return endpoints;
        }
    }
}
