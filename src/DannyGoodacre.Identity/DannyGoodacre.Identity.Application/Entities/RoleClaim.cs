using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Entities;

public sealed class RoleClaim
{
    [Key]
    public int Id { get; set; }

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public int ClaimId { get; set; }

    public Claim Claim { get; set; } = null!;
}
