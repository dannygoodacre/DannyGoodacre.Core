using System.Security.Claims;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain;
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

        foreach (string role in profile.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        foreach (ClaimDefinition claim in profile.Claims)
        {
            identity.AddClaim(new Claim(claim.Type, claim.Value));
        }

        return new ClaimsPrincipal(identity);
    }
}
