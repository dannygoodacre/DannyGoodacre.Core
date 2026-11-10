namespace DannyGoodacre.Identity.Domain.Entities;

public sealed class RoleClaim
{
    public int RoleId { get; set; }

    public int ClaimId { get; set; }

    public Claim Claim { get; set; } = null!;
}
