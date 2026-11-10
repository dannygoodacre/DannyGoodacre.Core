namespace DannyGoodacre.Identity;

internal interface IRolePermissionRegistry
{
    IReadOnlyDictionary<string, List<string>> RoleMappings { get; }
}

internal sealed class RolePermissionRegistry(IReadOnlyDictionary<string, List<string>> roleMappings)
    : IRolePermissionRegistry
{
    public IReadOnlyDictionary<string, List<string>> RoleMappings { get; } = roleMappings;
}
