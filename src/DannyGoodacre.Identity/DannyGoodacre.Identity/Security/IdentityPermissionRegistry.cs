namespace DannyGoodacre.Identity.Security;

public interface IIdentityPermissionRegistry
{
    IReadOnlyCollection<string> AllPermissions { get; }

    void Register(params string[] permissions);
}

public class IdentityPermissionRegistry : IIdentityPermissionRegistry
{
    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase)
    {
        BuiltInPermissions.ClaimsCreate,
        BuiltInPermissions.ClaimsRead,
        BuiltInPermissions.ClaimsDelete,
        BuiltInPermissions.RolesCreate,
        BuiltInPermissions.RolesRead,
        BuiltInPermissions.RolesDelete,
        BuiltInPermissions.UsersDelete
    };

    public const string PermissionClaimType = "Permission";

    public IReadOnlyCollection<string> AllPermissions
        => _permissions;

    public void Register(params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            _permissions.Add(permission);
        }
    }
}
