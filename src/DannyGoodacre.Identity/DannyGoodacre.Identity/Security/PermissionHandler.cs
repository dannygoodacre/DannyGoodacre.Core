using Microsoft.AspNetCore.Authorization;

namespace DannyGoodacre.Identity.Security;

internal sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        bool hasPermission = context.User.HasClaim(x => x.Type == "Permission" && x.Value == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
