using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Services;

internal sealed class SignInManager(Microsoft.AspNetCore.Identity.SignInManager<IdentityUser> signInManager)
    : ISignInManager
{
    public async Task<Result> PasswordSignInAsync(string username, string password)
    {
        var signInResult = await signInManager.PasswordSignInAsync(username, password, false, false);

        return signInResult.ToResult();
    }

    public Task SignOutAsync()
        => signInManager.SignOutAsync();
}
