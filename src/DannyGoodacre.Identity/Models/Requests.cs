namespace DannyGoodacre.Identity.Models;

public sealed record SignInRequest(string Username, string Password, bool RememberMe = false);

public sealed record PasswordResetRequest(string Username, string Token, string NewPassword);

public sealed record RegistrationRequest(string Username, string Password, string InviteCode, string? Email = null);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
