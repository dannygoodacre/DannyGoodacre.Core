namespace DannyGoodacre.Identity.Core;

public class Claim
{
    public string Id { get; set; }

    public required string UserId { get; set; }

    public required string ClaimType { get; set; }

    public required string ClaimValue { get; set; }
}
