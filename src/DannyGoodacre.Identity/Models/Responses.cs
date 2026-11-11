namespace DannyGoodacre.Identity.Models;

public sealed record UserResponse(Guid Id, string Username, string? Email, bool IsAdmin, bool IsEnabled);

/// <summary>
/// A password reset token for the admin to pass on to the user.
/// </summary>
public sealed record PasswordResetTokenResponse(string Username, string Token);

public sealed record InviteCodeResponse(Guid Code);
