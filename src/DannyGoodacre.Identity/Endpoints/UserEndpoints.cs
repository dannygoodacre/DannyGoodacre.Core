using System.Security.Claims;
using DannyGoodacre.Identity.Accounts;
using DannyGoodacre.Identity.Data;
using DannyGoodacre.Identity.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Endpoints;

internal static class UserEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("users", RegisterAsync);

        group.MapGet("users/me", GetCurrentUserAsync).RequireAuthorization();

        group.MapPut("users/me/password", ChangePasswordAsync).RequireAuthorization();

        group.MapDelete("users/{id:guid}", DeleteAsync).RequireAuthorization();
    }

    private static async Task<IResult> RegisterAsync(
        RegistrationRequest request,
        UserManager<User> userManager,
        IIdentityContextAccessor contextAccessor,
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        if (Problems.Required(
                (nameof(request.Username), request.Username),
                (nameof(request.Password), request.Password),
                (nameof(request.InviteCode), request.InviteCode)) is { } invalid)
        {
            return invalid;
        }

        var inviteCode = await contextAccessor.Context.Set<InviteCode>()
            .AsNoTracking()
            .SingleAsync(cancellationToken);

        if (!Guid.TryParse(request.InviteCode, out var code) || code != inviteCode.Code)
        {
            return Problems.Forbidden("Invalid invite code.");
        }

        var user = new User
        {
            UserName = request.Username,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return Problems.From(result);
        }

        // Built from the request path rather than a route name, so this group does not depend on the admin group.
        var location = $"{httpRequest.PathBase}{httpRequest.Path.Value?.TrimEnd('/')}/{user.Id}";

        return Results.Created(location, await user.ToResponseAsync(userManager));
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        var user = await userManager.FindUserAsync(principal);

        if (user is not null)
        {
            return Results.Ok(await user.ToResponseAsync(userManager));
        }
        
        await signInManager.SignOutAsync();

        return Problems.Unauthorized("The user no longer exists.");
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        if (Problems.Required(
                (nameof(request.CurrentPassword), request.CurrentPassword),
                (nameof(request.NewPassword), request.NewPassword)) is { } invalid)
        {
            return invalid;
        }

        var user = await userManager.FindUserAsync(principal);

        if (user is null)
        {
            await signInManager.SignOutAsync();

            return Problems.Unauthorized("The user no longer exists.");
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            return Problems.From(result);
        }

        // The new security stamp ends the user's other sessions; reissue this one so the caller stays signed in.
        await signInManager.RefreshSignInAsync(user);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ClaimsPrincipal principal,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        bool isSelf = principal.GetUserId() == id;

        if (!isSelf && !principal.IsInRole(IdentityDefaults.AdminRole))
        {
            return Problems.Forbidden("Only admins can delete other users.");
        }

        var user = await userManager.FindUserAsync(id);

        if (user is null)
        {
            return Problems.UserNotFound();
        }

        if (await userManager.IsLastAdminAsync(user))
        {
            return Problems.LastAdmin();
        }

        IdentityResult result;

        try
        {
            result = await userManager.DeleteAsync(user);
        }
        catch (DbUpdateException)
        {
            // Telling foreign key violations apart from other database errors is provider-specific, so every failure
            // is reported as related data.
            return Problems.Conflict("The user has related data and cannot be deleted. Disable them instead.");
        }

        if (!result.Succeeded)
        {
            return Problems.From(result);
        }

        if (isSelf)
        {
            await signInManager.SignOutAsync();
        }

        return Results.NoContent();
    }
}
