namespace DannyGoodacre.Identity.Models;

public sealed record CreateClaimRequest
{
    public required string Type { get; init; }

    public required string Value { get; init; }
}
