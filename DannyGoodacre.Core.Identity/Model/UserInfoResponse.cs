namespace DannyGoodacre.Core.Identity.Model;

public sealed record UserInfoResponse
{
    public required string Username { get; init; }

    public required bool IsAccountConfirmed { get; init; }
}
