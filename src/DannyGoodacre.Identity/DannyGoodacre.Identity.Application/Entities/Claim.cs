using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Entities;

public class Claim
{
    [Key]
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public required string Type { get; set; }

    public required string Value { get; set; }
}
