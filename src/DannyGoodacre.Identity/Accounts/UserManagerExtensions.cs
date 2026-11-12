using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace DannyGoodacre.Identity.Accounts;

internal static class UserManagerExtensions
{
    extension(UserManager<User> userManager)
    {
        public async Task<User?> FindUserAsync(ClaimsPrincipal principal)
            => principal.GetUserId() is { } id 
                ? await userManager.FindByIdAsync(id.ToString()) 
                : null;

        public Task<User?> FindUserAsync(Guid id)
            => userManager.FindByIdAsync(id.ToString());

        public async Task<int> CountEnabledAdminsAsync()
            => (await userManager.GetUsersInRoleAsync(IdentityDefaults.AdminRole)).Count(admin => !admin.IsDisabled);

        /// <summary>
        /// Whether <paramref name="user"/> is the only enabled member of <see cref="IdentityDefaults.AdminRole"/>.
        /// </summary>
        public async Task<bool> IsLastAdminAsync(User user)
            => !user.IsDisabled
               && await userManager.IsInRoleAsync(user, IdentityDefaults.AdminRole)
               && await userManager.CountEnabledAdminsAsync() <= 1;
    }
}
