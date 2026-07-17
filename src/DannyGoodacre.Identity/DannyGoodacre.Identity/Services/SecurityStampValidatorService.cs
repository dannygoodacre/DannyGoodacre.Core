using System.Security.Claims;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Configuration;
using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity.Services;

internal sealed class SecurityStampValidatorService(IValidateSecurityStamp validateSecurityStamp,
                                                    IOptions<Configuration.IdentityOptions> options)
    : CookieAuthenticationEvents
{
    private readonly Configuration.IdentityOptions _options = options.Value;

    public async override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        ClaimsPrincipal? principal = context.Principal;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        DateTimeOffset? issuedAt = context.Properties.IssuedUtc;

        if (issuedAt.HasValue && now < issuedAt.Value.AddMinutes(_options.SecurityStampValidationIntervalInMinutes))
        {
            return;
        }

        string? username = principal.Identity.Name;

        string? securityStamp = principal.FindFirstValue("SecurityStamp");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(securityStamp))
        {
            await RejectAndSignOutAsync(context);

            return;
        }

        Result<bool> result = await validateSecurityStamp.ExecuteAsync(username, securityStamp);

        if (!result.IsSuccess)
        {
            return;
        }

        if (!result.Value)
        {
            await RejectAndSignOutAsync(context);

            return;
        }

        context.ShouldRenew = true;
    }

    private async static Task RejectAndSignOutAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();

        await context.HttpContext.SignOutAsync();
    }
}
