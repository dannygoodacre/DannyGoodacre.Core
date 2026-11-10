using System.Security.Claims;
using DannyGoodacre.Identity.Application.Abstractions.Repositories;
using DannyGoodacre.Identity.Configuration;
using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity.Services;

internal sealed class SecurityStampValidatorService(IUserRepository userRepository, IOptions<IdentityOptions> options)
    : CookieAuthenticationEvents
{
    private readonly SecurityStampOptions _options = options.Value.SecurityStamp;

    public async override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        ClaimsPrincipal? principal = context.Principal;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        DateTimeOffset? issuedAt = context.Properties.IssuedUtc;

        if (issuedAt.HasValue && now < issuedAt.Value.Add(_options.ValidationInterval))
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

        User? user = await userRepository.GetAsync(username, context.HttpContext.RequestAborted);

        if (user is null || !string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal))
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
