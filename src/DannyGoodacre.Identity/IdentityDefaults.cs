namespace DannyGoodacre.Identity;

public static class IdentityDefaults
{
    /// <summary>
    /// The role whose members can do everything: manage accounts, admins, and the invite code.
    /// </summary>
    public const string AdminRole = "Admin";

    /// <summary>
    /// An authorization policy that requires <see cref="AdminRole"/>. Use it on your own admin-only endpoints and hubs.
    /// </summary>
    public const string AdminPolicy = "Admin";
}
