using DannyGoodacre.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace DannyGoodacre.Identity.Accounts;

internal static class UserExtensions
{
    /// <summary>
    /// Disabling uses Identity's own lockout: a disabled account is locked out until <see cref="DisabledUntil"/>, which no
    /// failed sign-in or expiring lockout ever reaches.
    /// </summary>
    public static readonly DateTimeOffset DisabledUntil = new(9999, 1, 1, 0, 0, 0, TimeSpan.Zero);

    extension(User user)
    {
        public bool IsDisabled => user.LockoutEnd >= DisabledUntil;

        public async Task<UserResponse> ToResponseAsync(UserManager<User> userManager)
            => new(
                user.Id,
                user.UserName ?? "",
                user.Email,
                await userManager.IsInRoleAsync(user, IdentityDefaults.AdminRole),
                !user.IsDisabled);
    }
}
