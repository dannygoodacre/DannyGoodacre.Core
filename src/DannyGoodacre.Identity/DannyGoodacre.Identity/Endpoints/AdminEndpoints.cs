using DannyGoodacre.Identity.Application;
using DannyGoodacre.Identity.Application.Abstractions.Repositories;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Security;
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

            group.MapGet("claims", async Task<IResult> (IClaimRepository claimRepository,
                                                        CancellationToken cancellationToken) =>
            {
                List<Claim> claims = await claimRepository.GetAllAsync(cancellationToken);

                return Results.Ok(claims.Select(x => x.ToResponse()));
            })
            .RequireAuthorization($"Permission:{BuiltInPermissions.ClaimsRead}");

            group.MapGet("claims/{id:guid}", async Task<IResult> (IClaimRepository claimRepository,
                                                                  [FromRoute] Guid id,
                                                                  CancellationToken cancellationToken) =>
            {
                Claim? claim = await claimRepository.GetAsync(id, cancellationToken);

                return claim is null
                    ? Results.NotFound()
                    : Results.Ok(claim.ToResponse());
            })
            .WithName("GetClaim")
            .RequireAuthorization($"Permission:{BuiltInPermissions.ClaimsRead}");

            group.MapPost("roles", async Task<IResult> (IRoleRepository roleRepository,
                                                        IClaimRepository claimRepository,
                                                        AddRoleRequest request,
                                                        CancellationToken cancellationToken) =>
            {
                if (await roleRepository.ExistsAsync(request.Name, cancellationToken))
                {
                    return Results.Conflict("Role already exists");
                }

                Dictionary<Guid, int> claimIdMap = await claimRepository.GetIdMapAsync(request.ClaimIds, cancellationToken);

                List<Guid> missingClaimIds = request.ClaimIds
                    .Where(id => !claimIdMap.ContainsKey(id))
                    .ToList();

                if (missingClaimIds.Count > 0)
                {
                    return Results.BadRequest($"Missing claims: {string.Join(", ", missingClaimIds)}.");
                }

                _ = roleRepository.Add(new Role
                {
                    PublicId = Guid.NewGuid(),
                    Name = request.Name,
                    Claims = request.ClaimIds.Select(claimId => new RoleClaim
                    {
                        ClaimId = claimIdMap[claimId]
                    }).ToList()
                });

                return Results.NoContent();
            })
            .RequireAuthorization($"Permission:{BuiltInPermissions.RolesCreate}");

            group.MapGet("roles/{id:guid}", async Task<IResult> (IRoleRepository roleRepository,
                                                                 [FromRoute] Guid id,
                                                                 CancellationToken cancellationToken) =>
            {
                Role? role = await roleRepository.GetAsync(id, cancellationToken);

                return role is null
                    ? Results.NotFound()
                    : Results.Ok(role.ToResponse());
            })
            .WithName("GetRole")
            .RequireAuthorization($"Permission:{BuiltInPermissions.RolesRead}");

            group.MapDelete("roles/{id:guid}", async Task<IResult> (IRoleRepository roleRepository,
                                                                    [FromRoute] Guid id,
                                                                    CancellationToken cancellationToken) =>
                {
                    Role? role = await roleRepository.GetAsync(id, cancellationToken);

                    if (role is null)
                    {
                        return Results.NotFound();
                    }

                    roleRepository.Remove(role);

                    return Results.NoContent();
                })
            .RequireAuthorization($"Permission:{BuiltInPermissions.RolesDelete}");

            return endpoints;
        }
    }
}
