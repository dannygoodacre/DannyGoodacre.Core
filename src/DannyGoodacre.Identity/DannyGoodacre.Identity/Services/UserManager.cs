using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Services;

internal sealed class UserManager(Microsoft.AspNetCore.Identity.UserManager<IdentityUser> userManager)
    : IUserManager<IdentityUser>
{

    public async Task<Result> AddToRoleAsync(IdentityUser user, string roleName)
    {
        var identityResult = await userManager.AddToRoleAsync(user, roleName);

        return identityResult.ToResult();
    }

    public Task<IdentityUser?> FindByIdAsync(string userId)
        => userManager.FindByIdAsync(userId);

    public Task<IdentityUser?> FindByNameAsync(string username)
        => userManager.FindByNameAsync(username);

    public async Task<Result> ChangePasswordAsync(IdentityUser user, string currentPassword, string newPassword)
    {
        var identityResult = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        return identityResult.ToResult();
    }

    public async Task<Result> CreateAsync(IdentityUser user, string password)
    {
        var identityResult = await userManager.CreateAsync(user, password);

        return identityResult.ToResult();
    }

    public async Task<Result> UpdateAsync(IdentityUser user)
    {
        var identityResult = await userManager.UpdateAsync(user);

        return identityResult.ToResult();
    }

    public Task<bool> IsEmailConfirmedAsync(IdentityUser user)
        => userManager.IsEmailConfirmedAsync(user);
}
