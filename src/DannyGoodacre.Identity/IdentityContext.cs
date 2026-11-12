using DannyGoodacre.Identity.Data;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity;

/// <summary>
/// The base class for the application's <see cref="DbContext"/>. It holds the Identity tables, the invite code, and
/// a table for Data Protection keys, so the application's migrations include all of them.
/// </summary>
public abstract class IdentityContext : IdentityDbContext<User, Role, Guid>, IDataProtectionKeyContext
{
    protected IdentityContext(DbContextOptions options) : base(options) { }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<InviteCode>(inviteCode =>
        {
            inviteCode.ToTable("InviteCodes");
            inviteCode.Property(code => code.Id).ValueGeneratedNever();
        });
    }
}
