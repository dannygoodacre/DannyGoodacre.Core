using DannyGoodacre.Identity.Accounts;
using DannyGoodacre.Identity.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace DannyGoodacre.Identity.Endpoints;

internal static class SessionEndpoints
{
    private const string InvalidCredentials = "Invalid username or password.";

    private const string InvalidResetToken = "Invalid username or token.";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("sessions", SignInAsync);

        group.MapDelete("sessions", SignOutAsync).RequireAuthorization();

        group.MapPost("password-reset", ResetPasswordAsync);
    }

    private static async Task<IResult> SignInAsync(
        SignInRequest request,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        TimeProvider timeProvider,
        HttpContext httpContext)
    {
        if (Problems.Required((nameof(request.Username), request.Username), (nameof(request.Password), request.Password)) is { } invalid)
        {
            return invalid;
        }

        var user = await userManager.FindByNameAsync(request.Username);

        if (user is null)
        {
            return Problems.Unauthorized(InvalidCredentials);
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return Results.NoContent();
        }

        if (!result.IsLockedOut)
        {
            return Problems.Unauthorized(InvalidCredentials);
        }

        if (user.IsDisabled)
        {
            return Problems.Forbidden("The account is disabled.");
        }

        if (user.LockoutEnd is { } lockoutEnd)
        {
            double seconds = Math.Ceiling((lockoutEnd - timeProvider.GetUtcNow()).TotalSeconds);

            httpContext.Response.Headers[HeaderNames.RetryAfter] = Math.Max(1, seconds).ToString("0");
        }

        return Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            detail: "The account is locked out after too many failed sign-in attempts.");
    }

    private static async Task<IResult> SignOutAsync(SignInManager<User> signInManager)
    {
        await signInManager.SignOutAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> ResetPasswordAsync(PasswordResetRequest request, UserManager<User> userManager)
    {
        if (Problems.Required(
                (nameof(request.Username), request.Username),
                (nameof(request.Token), request.Token),
                (nameof(request.NewPassword), request.NewPassword)) is { } invalid)
        {
            return invalid;
        }

        var user = await userManager.FindByNameAsync(request.Username);

        // A disabled user's tokens are already invalid, because disabling changes the security stamp. The check also
        // stops the lockout reset below from re-enabling them.
        if (user is null || user.IsDisabled)
        {
            return InvalidToken();
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            return result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.InvalidToken))
                ? InvalidToken()
                : Problems.From(result);
        }

        // A locked-out user is the likely caller.
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);

        return Results.NoContent();

        static IResult InvalidToken()
            => Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: InvalidResetToken);
    }
}
