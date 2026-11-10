using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity.Security;

internal sealed class DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public async override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            return await base.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder();

        string permissionValue = policyName["Permission:".Length..];

        policy.AddRequirements(new PermissionRequirement(permissionValue));

        return policy.Build();
    }
}
