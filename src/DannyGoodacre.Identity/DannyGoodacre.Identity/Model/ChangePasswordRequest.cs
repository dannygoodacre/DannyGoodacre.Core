namespace DannyGoodacre.Identity.Model;

public sealed record ChangePasswordRequest
{
    public required string OldPassword { get; init; }

    public required string NewPassword { get; init; }
}
