using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Tests.Infrastructure;

public sealed class TestIdentityContext(DbContextOptions<TestIdentityContext> options) : IdentityContext(options)
{
    public DbSet<GameRecord> GameRecords => Set<GameRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<GameRecord>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GameRecord
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }
}
