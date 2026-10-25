namespace DannyGoodacre.Identity.Application.Models;

public sealed record RoleResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyCollection<ClaimResponse> Claims { get; init; }
}
