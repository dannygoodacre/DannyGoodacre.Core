using DannyGoodacre.Identity.Domain;

namespace DannyGoodacre.Identity.Responses;

public record UserSecurityProfileResponse(Guid Id, string Username, IReadOnlyCollection<string> Roles, IReadOnlyCollection<ClaimDefinition> Claims);
