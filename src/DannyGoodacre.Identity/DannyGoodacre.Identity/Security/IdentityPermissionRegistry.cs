namespace DannyGoodacre.Identity.Security;

public interface IIdentityPermissionRegistry
{
    IReadOnlyCollection<string> AllPermissions { get; }
}

internal sealed class IdentityPermissionRegistry : IIdentityPermissionRegistry
{
    public const string PermissionClaimType = "Permission";

    private readonly HashSet<string> _allPermissions;

    public IdentityPermissionRegistry(IEnumerable<IPermissionProvider> permissionProviders)
    {
        _allPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BuiltInPermissions.ClaimsRead,
            BuiltInPermissions.RolesCreate,
            BuiltInPermissions.RolesRead,
            BuiltInPermissions.RolesDelete,
            BuiltInPermissions.UsersDelete
        };

        foreach (var provider in permissionProviders)
        {
            foreach (string permission in provider.GetPermissions())
            {
                if (!string.IsNullOrWhiteSpace(permission))
                {
                    _allPermissions.Add(permission.Trim());
                }
            }
        }
    }

    public IReadOnlyCollection<string> AllPermissions => _allPermissions;
}
