using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data;

public class IdentityContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    public DbSet<Claim> Claims { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<UserClaim> UserClaims { get; set; }

    public DbSet<RoleClaim> RoleClaims { get; set; }
}
