using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Abstractions;

public interface IUserStore<in TUser> where TUser : IdentityUser
{
    Task SetUsernameAsync(TUser user, string username,  CancellationToken cancellationToken);
}
