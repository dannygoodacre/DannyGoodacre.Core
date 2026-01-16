using Microsoft.AspNetCore.Identity;

namespace DannyGoodacre.Identity.Services;

internal sealed class UserStore(IUserStore<DannyGoodacre.Identity.Core.IdentityUser> userStore): Application.Abstractions.IUserStore<DannyGoodacre.Identity.Core.IdentityUser>
{

    public Task SetUsernameAsync(Core.IdentityUser user, string username, CancellationToken cancellationToken)
        => userStore.SetUserNameAsync(user, username, cancellationToken);
}
