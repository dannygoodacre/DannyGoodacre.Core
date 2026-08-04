using System.Security.Claims;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DannyGoodacre.Identity.Services;

internal interface IClaimService
{
    ClaimsPrincipal CreateClaimsPrincipal(UserSecurityProfileResponse profileResponse);
}

internal sealed class ClaimService : IClaimService
{
    public ClaimsPrincipal CreateClaimsPrincipal(UserSecurityProfileResponse profileResponse)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, profileResponse.Id.ToString()));

        identity.AddClaim(new Claim(ClaimTypes.Name, profileResponse.Username));

        foreach (string role in profileResponse.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        foreach (ClaimDefinition claim in profileResponse.Claims)
        {
            identity.AddClaim(new Claim(claim.Type, claim.Value));
        }

        return new ClaimsPrincipal(identity);
    }
}
