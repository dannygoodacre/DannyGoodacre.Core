using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity;

public class DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{

    public async override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            return await base.GetPolicyAsync(policyName);
        }

        var permissionValue = policyName["Permission:".Length..];

        var policy = new AuthorizationPolicyBuilder();

        policy.AddRequirements(new PermissionRequirement(permissionValue));

        return policy.Build();

    }
}
