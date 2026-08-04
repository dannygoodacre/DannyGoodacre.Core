using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Entities;

public sealed class UserRole
{
    [Key]
    public int Id { get; set; }

    public required int RoleId { get; set; }

    public required Role Role { get; set; }
}
