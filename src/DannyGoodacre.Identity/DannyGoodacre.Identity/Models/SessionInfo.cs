namespace DannyGoodacre.Identity.Models;

public sealed record SessionInfo
{
    public required Guid? UserId { get; init; }

    public required string? Username { get; set; }

    public required bool IsAuthenticated { get; set; }

    public required List<ClaimPair> Claims { get; set; }

    public List<string> Roles { get; init; } = [];
}
