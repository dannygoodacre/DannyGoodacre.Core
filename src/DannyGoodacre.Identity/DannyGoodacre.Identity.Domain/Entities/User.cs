using System.ComponentModel.DataAnnotations;

namespace DannyGoodacre.Identity.Domain.Entities;

public sealed class User
{
    [Key]
    public int Id { get; init; }

    public Guid PublicId { get; set; }

    public required string Username { get; set; }

    public required bool IsApproved { get; set; }

    public required string PasswordHash  { get; set; }

    public DateTime LastLogin { get; set; }

    public required string SecurityStamp { get; set; }

    public required string ConcurrencyStamp { get; set; }

    public ICollection<Role> Roles { get; set; } = [];

    public ICollection<UserClaim> Claims { get; set; } = [];
}
