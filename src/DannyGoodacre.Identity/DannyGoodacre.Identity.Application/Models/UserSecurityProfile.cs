namespace DannyGoodacre.Identity.Application.Models;

public sealed record UserSecurityProfile
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }

    public required string SecurityStamp { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    // TODO: Hacky, replace with a dictionary or something.
    public required IReadOnlyCollection<(string Type, string Value)> Claims { get; init; }
}
