using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity;

public class IdentityContext(DbContextOptions options)
    : IdentityDbContext<Core.IdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Core.IdentityUser>(entity =>
        {
            entity.ToTable("Users");

            entity.Property(x => x.EmailConfirmed).HasColumnName("IsUserConfirmed");

            entity.Ignore(x => x.PhoneNumber);
            entity.Ignore(x => x.PhoneNumberConfirmed);
            entity.Ignore(x => x.TwoFactorEnabled);
        });

        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
    }
}
