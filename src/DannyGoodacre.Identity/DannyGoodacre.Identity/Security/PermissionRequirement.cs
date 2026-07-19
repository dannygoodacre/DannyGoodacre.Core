using Microsoft.AspNetCore.Authorization;

namespace DannyGoodacre.Identity.Security;

internal sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;

}
