using DannyGoodacre.Identity.Application.Abstractions.Data;
using DannyGoodacre.Identity.Core;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data;

public class IdentityContext(DbContextOptions options) : DbContext(options), IIdentityContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<Claim> Claims { get; set; }

    public new Task<int> SaveChangesAsync()
        => base.SaveChangesAsync();
}
