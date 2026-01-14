using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Core;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Services;

internal sealed class UserStore(UserStore<IdentityUser> userStore): IUserStore<IdentityUser>
{

    public Task SetUsernameAsync(IdentityUser user, string username, CancellationToken cancellationToken)
        => userStore.SetUserNameAsync(user, username, cancellationToken);
}
