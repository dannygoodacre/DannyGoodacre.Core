using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data;

public class IdentityContext(DbContextOptions options) : DbContext(options), IStateUnit
{
    public DbSet<User> Users { get; set; }

    public DbSet<Claim> Claims { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<UserClaim> UserClaims { get; set; }

    public DbSet<RoleClaim> RoleClaims { get; set; }

    public Task<int> SaveChangesAsync()
        => base.SaveChangesAsync();
}
