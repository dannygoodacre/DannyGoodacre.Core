namespace DannyGoodacre.Identity.Domain.Entities;

public sealed class UserClaim
{
    public int UserId { get; set; }

    public int ClaimId { get; set; }

    public Claim Claim { get; set; } = null!;
}
