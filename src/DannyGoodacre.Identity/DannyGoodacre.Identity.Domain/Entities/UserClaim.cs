using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Domain.Entities;

public sealed class UserClaim
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int ClaimId { get; set; }

    public Claim Claim { get; set; } = null!;

    public required string Type { get; set; }

    public required string Value { get; set; }
}
