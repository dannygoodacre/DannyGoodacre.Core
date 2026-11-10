namespace DannyGoodacre.Identity.Responses;

public sealed record ClaimResponse(Guid Id, string Type, string Value, bool IsDisabled);
