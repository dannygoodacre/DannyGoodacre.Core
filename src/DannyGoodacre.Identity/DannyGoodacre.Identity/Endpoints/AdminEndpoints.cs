using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Security;
using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DannyGoodacre.Identity.Endpoints;

internal static class AdminEndpoints
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapAdminEndpoints()
        {
            RouteGroupBuilder group = endpoints
                .MapGroup("")
                .WithTags("Identity: Admin")
                .RequireAuthorization();

            group.MapGet("claims", async Task<IResult> ([FromServices] IGetAllClaims getAllClaims,
                                                        CancellationToken cancellationToken) =>
            {
                Result<List<ClaimResponse>> result = await getAllClaims.ExecuteAsync(cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization($"Permission:{BuiltInPermissions.ClaimsRead}");

            group.MapGet("claims/{id:guid}", async Task<IResult> ([FromServices] IGetClaim getClaim,
                                                                  [FromRoute] Guid id,
                                                                  CancellationToken cancellationToken) =>
            {
                Result<ClaimResponse> result = await getClaim.ExecuteAsync(id, cancellationToken);

                return result.ToHttpResponse();
            })
            .WithName("GetClaim")
            .RequireAuthorization($"Permission:{BuiltInPermissions.ClaimsRead}");

            group.MapPost("roles", async Task<IResult> ([FromServices] IAddRole addRole,
                                                        [FromBody] AddRoleRequest request,
                                                        CancellationToken cancellationToken) =>
            {
                Result result = await addRole.ExecuteAsync(request.ToCommand(), cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization($"Permission:{BuiltInPermissions.RolesCreate}");

            group.MapGet("roles/{id:guid}", async Task<IResult> ([FromServices] IGetRole getRole,
                                                                 [FromRoute] Guid id,
                                                                 CancellationToken cancellationToken) =>
            {
                Result<RoleResponse> result = await getRole.ExecuteAsync(id, cancellationToken);

                return result.ToHttpResponse();
            })
            .WithName("GetRole")
            .RequireAuthorization($"Permission:{BuiltInPermissions.RolesRead}");

            group.MapDelete("roles/{id:guid}", async Task<IResult> ([FromServices] IDeleteRole deleteRole,
                                                                    [FromRoute] Guid id,
                                                                    CancellationToken cancellationToken) =>
            {
                Result result = await deleteRole.ExecuteAsync(id, cancellationToken);

                return result.ToHttpResponse();
            })
            .RequireAuthorization($"Permission:{BuiltInPermissions.RolesDelete}");

            return endpoints;
        }
    }
}
