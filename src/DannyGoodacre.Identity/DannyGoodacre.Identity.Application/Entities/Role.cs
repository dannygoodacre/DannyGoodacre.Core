using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Entities;

public sealed class Role
{
    [Key]
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public required string Name { get; set; }

    public ICollection<RoleClaim> Claims { get; set; } = [];
}
