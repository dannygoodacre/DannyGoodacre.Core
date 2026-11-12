using DannyGoodacre.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace DannyGoodacre.Identity.Accounts;

internal static class UserExtensions
{
    /// <summary>
    /// Disabling uses Identity's own lockout: a disabled account is locked out until this date, which no failed sign-in
    /// or expiring lockout ever reaches.
    /// </summary>
    private static readonly DateTimeOffset DisabledUntil = new(9999, 1, 1, 0, 0, 0, TimeSpan.Zero);

    extension(User user)
    {
        public bool IsDisabled => user.LockoutEnd >= DisabledUntil;

        /// <summary>
        /// Marks the account disabled. Save it with <c>UserManager.UpdateSecurityStampAsync</c>: security stamp
        /// validation does not check lockout, so only a new stamp ends the user's sessions.
        /// </summary>
        public void Disable()
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DisabledUntil;
        }

        public void Enable()
        {
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
        }

        public async Task<UserResponse> ToResponseAsync(UserManager<User> userManager)
            => new(
                user.Id,
                user.UserName ?? "",
                user.Email,
                await userManager.IsInRoleAsync(user, IdentityDefaults.AdminRole),
                !user.IsDisabled);
    }
}
