using DannyGoodacre.Identity.Domain;

namespace DannyGoodacre.Identity.Application.Models;

public sealed record UserSecurityProfileResponse
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public required IReadOnlyCollection<ClaimDefinition> Claims { get; init; }
}
