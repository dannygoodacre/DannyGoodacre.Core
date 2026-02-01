namespace DannyGoodacre.Identity.Core;

public class User
{
    public string Id { get; set; } = null!;

    public required string Username { get; set; }

    public required bool IsApproved { get; set; }

    public required string PasswordHash  { get; set; }

    public DateTime LastLogin { get; set; }

    public required string SecurityStamp { get; set; }

    public required string ConcurrencyStamp { get; set; }

    public ICollection<Role> Roles { get; set; }
}
