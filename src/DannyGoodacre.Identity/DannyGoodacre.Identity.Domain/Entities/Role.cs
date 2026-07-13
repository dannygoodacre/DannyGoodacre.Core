using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Domain.Entities;

public sealed class Role
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public ICollection<RoleClaim> Claims { get; set; } = [];
}
