using System.Security.Claims;
using DannyGoodacre.Identity.Application.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DannyGoodacre.Identity.Services;

internal interface IClaimService
{
    ClaimsPrincipal CreateClaimsPrincipal(UserSecurityProfile profile);
}

internal sealed class ClaimService : IClaimService
{
    public ClaimsPrincipal CreateClaimsPrincipal(UserSecurityProfile profile)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, profile.Id.ToString()));

        identity.AddClaim(new Claim(ClaimTypes.Name, profile.Username));

        identity.AddClaim(new Claim("SecurityStamp", profile.SecurityStamp));

        foreach (string role in profile.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        foreach ((string type, string value) in profile.Claims)
        {
            identity.AddClaim(new Claim(type, value));
        }

        return new ClaimsPrincipal(identity);
    }
}
