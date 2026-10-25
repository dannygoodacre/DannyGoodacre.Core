namespace DannyGoodacre.Identity;

public interface IPermissionProvider
{
    IReadOnlyCollection<string> GetPermissions();
}
