namespace DannyGoodacre.Identity.Application.Models;

public sealed record ClaimResponse
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required string Value { get; init; }
}
