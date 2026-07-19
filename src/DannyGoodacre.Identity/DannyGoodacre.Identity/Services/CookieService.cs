using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace DannyGoodacre.Identity.Services;

internal interface ICookieService
{
    Task IssueCookieAsync(HttpContext httpContext, List<Claim> claims);

    Task RevokeCookieAsync(HttpContext httpContext);
}

internal sealed class CookieService : ICookieService
{

    public async Task IssueCookieAsync(HttpContext httpContext, List<Claim> claims)
    {
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // TODO: Cookie options
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
        };

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                      new ClaimsPrincipal(claimsIdentity),
                                      authProperties);
    }

    public Task RevokeCookieAsync(HttpContext httpContext)
        => httpContext.SignOutAsync();
}
