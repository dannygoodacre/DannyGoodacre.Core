namespace DannyGoodacre.Identity.Models;

public sealed record AddRoleRequest
{
    public required string Name { get; init; }

    public required List<Guid> ClaimIds { get; init; }
}
