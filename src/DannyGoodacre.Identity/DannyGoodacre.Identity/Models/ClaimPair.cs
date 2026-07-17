namespace DannyGoodacre.Identity.Models;

public sealed record ClaimPair
{
    public required string Type { get; init; }

    public required string Value { get; init; }
}
