using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Domain.Entities;

public class Claim
{
    [Key]
    public int Id { get; set; }

    public required string Type { get; set; }

    public required string Value { get; set; }
}
