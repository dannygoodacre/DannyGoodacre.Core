namespace DannyGoodacre.Identity.Responses;

public sealed record RoleResponse(Guid Id, string Name, IReadOnlyCollection<ClaimResponse> Claims, bool IsDisabled);
