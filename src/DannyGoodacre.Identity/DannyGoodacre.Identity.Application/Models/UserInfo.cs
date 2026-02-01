namespace DannyGoodacre.Identity.Application.Models;

public sealed record UserInfo
{
    public required string Username { get; init; }

    public required bool IsApproved  { get; init; }
}
