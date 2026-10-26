namespace DannyGoodacre.Identity.Domain.Entities;

public sealed class Role
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public required string Name { get; set; }

    public ICollection<RoleClaim> Claims { get; set; } = [];

    public bool IsDisabled { get; set; }
}
