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

internal static class AdminEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(IdentityDefaults.AdminPolicy);

        group.MapGet("users", ListUsersAsync);

        group.MapGet("users/{id:guid}", GetUserAsync);

        group.MapPut("users/{id:guid}/enabled", EnableAsync);

        group.MapDelete("users/{id:guid}/enabled", DisableAsync);

        group.MapPost("users/{id:guid}/password-reset", IssuePasswordResetTokenAsync);

        group.MapPut("users/{id:guid}/admin", MakeAdminAsync);

        group.MapDelete("users/{id:guid}/admin", RemoveAdminAsync);

        group.MapGet("invite-code", GetInviteCodeAsync);

        group.MapPost("invite-code", RotateInviteCodeAsync);
    }

    private static async Task<IResult> ListUsersAsync(UserManager<User> userManager, CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .OrderBy(user => user.NormalizedUserName)
            .ToListAsync(cancellationToken);

        var adminIds = (await userManager.GetUsersInRoleAsync(IdentityDefaults.AdminRole))
            .Select(admin => admin.Id)
            .ToHashSet();

        var responses = users.Select(user => 
            new UserResponse(
                user.Id, 
                user.UserName ?? "", 
                user.Email, 
                adminIds.Contains(user.Id),
                !user.IsDisabled));

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetUserAsync(Guid id, UserManager<User> userManager)
        => await userManager.FindUserAsync(id) is { } user
            ? Results.Ok(await user.ToResponseAsync(userManager))
            : Problems.UserNotFound();

    private static async Task<IResult> EnableAsync(Guid id, UserManager<User> userManager)
    {
        var user = await userManager.FindUserAsync(id);

        if (user is null)
        {
            return Problems.UserNotFound();
        }

        if (!user.IsDisabled)
        {
            return Results.NoContent();
        }

        user.Enable();

        var result = await userManager.UpdateAsync(user);

        return result.Succeeded 
            ? Results.NoContent() 
            : Problems.From(result);
    }

    private static async Task<IResult> DisableAsync(Guid id, ClaimsPrincipal principal, UserManager<User> userManager)
    {
        var user = await userManager.FindUserAsync(id);

        if (user is null)
        {
            return Problems.UserNotFound();
        }

        if (principal.GetUserId() == id)
        {
            return Problems.Conflict("You cannot disable your own account. Ask an admin.");
        }

        if (await userManager.IsLastAdminAsync(user))
        {
            return Problems.LastAdmin();
        }

        if (user.IsDisabled)
        {
            return Results.NoContent();
        }

        user.Disable();

        // Security stamp validation does not check lockout, so only a new stamp ends the user's sessions. This also
        // saves the lockout change.
        var result = await userManager.UpdateSecurityStampAsync(user);

        return result.Succeeded 
            ? Results.NoContent() 
            : Problems.From(result);
    }

    private static async Task<IResult> IssuePasswordResetTokenAsync(Guid id, ClaimsPrincipal principal, UserManager<User> userManager)
    {
        var user = await userManager.FindUserAsync(id);

        if (user is null)
        {
            return Problems.UserNotFound();
        }

        if (principal.GetUserId() == id)
        {
            return Problems.Conflict("Change your own password with PUT /users/me/password.");
        }

        if (user.IsDisabled)
        {
            return Problems.Conflict("The user is disabled, so a reset token would not let them sign in.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        return Results.Ok(new PasswordResetTokenResponse(user.UserName ?? "", token));
    }

    // Admin changes do not update the security stamp: a new stamp would sign the user out, and the principal is rebuilt
    // from the database at the next validation anyway.
    private static async Task<IResult> MakeAdminAsync(Guid id, UserManager<User> userManager)
    {
        var user = await userManager.FindUserAsync(id);

        if (user is null)
        {
            return Problems.UserNotFound();
        }

        if (await userManager.IsInRoleAsync(user, IdentityDefaults.AdminRole))
        {
            return Results.NoContent();
        }

        var result = await userManager.AddToRoleAsync(user, IdentityDefaults.AdminRole);

        return result.Succeeded 
            ? Results.NoContent() 
            : Problems.From(result);
    }

    private static async Task<IResult> RemoveAdminAsync(Guid id, UserManager<User> userManager)
    {
        var user = await userManager.FindUserAsync(id);

        if (user is null)
        {
            return Problems.UserNotFound();
        }

        if (!await userManager.IsInRoleAsync(user, IdentityDefaults.AdminRole))
        {
            return Results.NoContent();
        }

        if (await userManager.IsLastAdminAsync(user))
        {
            return Problems.LastAdmin();
        }

        var result = await userManager.RemoveFromRoleAsync(user, IdentityDefaults.AdminRole);

        return result.Succeeded 
            ? Results.NoContent() 
            : Problems.From(result);
    }

    private static async Task<IResult> GetInviteCodeAsync(IIdentityContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var inviteCode = await contextAccessor.Context.Set<InviteCode>()
            .AsNoTracking()
            .SingleAsync(cancellationToken);

        return Results.Ok(new InviteCodeResponse(inviteCode.Code));
    }

    private static async Task<IResult> RotateInviteCodeAsync(IIdentityContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Context;

        var inviteCode = await context.Set<InviteCode>().SingleAsync(cancellationToken);

        inviteCode.Code = Guid.NewGuid();

        await context.SaveChangesAsync(cancellationToken);

        return Results.Ok(new InviteCodeResponse(inviteCode.Code));
    }
}
