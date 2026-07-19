namespace DannyGoodacre.Identity.Domain;

public sealed record ClaimDefinition
{
    public required string Type { get; init; }

    public required string Value { get; init; }
}
