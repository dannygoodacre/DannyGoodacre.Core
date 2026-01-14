using DannyGoodacre.Core;
using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Abstractions;

public interface IUserManager<TUser> where TUser : IdentityUser
{
    Task<Result> AddToRoleAsync(TUser user, string roleName);

    Task<TUser?> FindByIdAsync(string userId);

    Task<TUser?> FindByNameAsync(string username);

    Task<Result> ChangePasswordAsync(TUser user, string currentPassword, string newPassword);

    Task<Result> CreateAsync(TUser user, string password);

    Task<Result> UpdateAsync(TUser user);

    Task<bool> IsEmailConfirmedAsync(TUser user);
}
