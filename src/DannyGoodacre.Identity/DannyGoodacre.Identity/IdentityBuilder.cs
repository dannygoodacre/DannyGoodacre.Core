using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity;

public sealed class IdentityBuilder(IServiceCollection services)
{
    private IServiceCollection Services { get; } = services;

    /// <summary>
    /// Registers a static or non-static class containing permission string constants.
    /// </summary>
    public IdentityBuilder AddPermissions(Type permissionType)
    {
        Services.AddSingleton<IPermissionProvider>(new TypeReflectionPermissionProvider(permissionType));
        return this;
    }

    /// <summary>
    /// Registers default role-to-permission mappings.
    /// </summary>
    public IdentityBuilder AddRoles(IReadOnlyDictionary<string, List<string>> roleMappings)
    {
        Services.AddSingleton<IRolePermissionRegistry>(new RolePermissionRegistry(roleMappings));
        return this;
    }
}
