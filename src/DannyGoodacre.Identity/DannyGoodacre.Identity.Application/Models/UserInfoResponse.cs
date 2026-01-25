namespace DannyGoodacre.Identity.Application.Models;

public sealed record UserInfoResponse
{
    public required string UserId  { get; init; }

    public required string Username { get; init; }

    public required bool IsApproved  { get; init; }
}
