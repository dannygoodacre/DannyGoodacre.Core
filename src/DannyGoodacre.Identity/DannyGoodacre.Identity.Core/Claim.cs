using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Core;

public class Claim
{
    [Key]
    public int Id { get; set; }

    public required string UserId { get; set; }

    public required string ClaimType { get; set; }

    public required string ClaimValue { get; set; }
}
