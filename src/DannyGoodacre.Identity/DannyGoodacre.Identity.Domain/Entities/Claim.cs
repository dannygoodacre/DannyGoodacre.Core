namespace DannyGoodacre.Identity.Domain.Entities;

public class Claim
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public required string Type { get; set; }

    public required string Value { get; set; }

    public bool IsDisabled { get; set; }
}
