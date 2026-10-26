namespace DannyGoodacre.Identity.Domain.Entities;

public sealed class UserRole
{
    public required int UserId { get; set; }

    public required int RoleId { get; set; }

    public required Role Role { get; set; }
}
