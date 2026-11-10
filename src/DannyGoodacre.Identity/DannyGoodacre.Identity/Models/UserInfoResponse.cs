namespace DannyGoodacre.Identity.Responses;

public record UserInfoResponse(Guid Id, string Username, bool IsApproved);
