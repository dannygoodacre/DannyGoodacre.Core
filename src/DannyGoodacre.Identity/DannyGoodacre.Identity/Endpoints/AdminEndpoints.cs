using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ClaimResponse = DannyGoodacre.Identity.Application.Models.ClaimResponse;

namespace DannyGoodacre.Identity.Endpoints;

internal static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("")
            .WithTags("Identity: Admin")
            .RequireAuthorization();

        group.MapPost("claims", async Task<IResult> ([FromServices] IAddClaim addClaim,
                                                     [FromBody] CreateClaimRequest request,
                                                     CancellationToken cancellationToken) =>
        {
            Result<ClaimResponse> result = await addClaim.ExecuteAsync(request.Type, request.Value, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ToHttpResponse();
            }

            ClaimResponse claim = result.Value;

            return Results.CreatedAtRoute("GetClaim", new { id = claim.Id }, claim);
        });

        group.MapGet("claims/{id:guid}", async Task<IResult> ([FromServices] IGetClaim getClaim,
                                                              [FromRoute] Guid id,
                                                              CancellationToken cancellationToken) =>
        {
            Result<ClaimResponse> result = await getClaim.ExecuteAsync(id, cancellationToken);

            return result.ToHttpResponse();
        })
        .WithName("GetClaim");

        group.MapPost("roles", async Task<IResult> ([FromServices] IAddRole addRole,
                                                    [FromBody] AddRoleRequest request,
                                                    CancellationToken cancellationToken) =>
        {
            Result result = await addRole.ExecuteAsync(request.Name, request.ClaimIds, cancellationToken);

            return result.ToHttpResponse();
        });

        group.MapGet("roles/{id:guid}", async Task<IResult> ([FromServices] IGetRole getRole,
                                                             [FromRoute] Guid id,
                                                             CancellationToken cancellationToken) =>
        {
            Result<RoleResponse> result = await getRole.ExecuteAsync(id, cancellationToken);

            return result.ToHttpResponse();
        })
        .WithName("GetRole");

        return endpoints;
    }
}
