using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity.Services;

internal interface ICookieService
{
    Task IssueCookieAsync(HttpContext httpContext, List<Claim> claims);

    Task RevokeCookieAsync(HttpContext httpContext);
}

internal sealed class CookieService(IOptions<Configuration.CookieOptions> options) : ICookieService
{
    private readonly Configuration.CookieOptions _options = options.Value;

    public async Task IssueCookieAsync(HttpContext httpContext, List<Claim> claims)
    {
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = _options.IsPersistent,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(_options.ExpireTimeSpanInMinutes),
            AllowRefresh = _options.SlidingExpiration,
            IssuedUtc = DateTimeOffset.UtcNow
        };

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                      claimsPrincipal,
                                      authProperties);
    }

    public Task RevokeCookieAsync(HttpContext httpContext)
        => httpContext.SignOutAsync();
}
