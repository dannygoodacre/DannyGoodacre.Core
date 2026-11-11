using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Example;

public sealed class ExampleContext(DbContextOptions<ExampleContext> options) : IdentityContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<Game> Games => Set<Game>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Configures the Identity and Data Protection tables, so it must run first.
        base.OnModelCreating(builder);

        builder.Entity<Player>(player =>
        {
            player.HasKey(p => p.UserId);

            // Restrict, so deleting a user with a player profile fails instead of erasing their history.
            player.HasOne<User>().WithOne().HasForeignKey<Player>(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

            player.HasMany(p => p.Games).WithOne().HasForeignKey(g => g.PlayerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

/// <summary>
/// Per-user data kept in the application's own table, keyed by <see cref="User"/>'s id.
/// </summary>
public sealed class Player
{
    public Guid UserId { get; init; }

    [MaxLength(100)]
    public required string DisplayName { get; set; }

    public int Chips { get; set; }

    public List<Game> Games { get; init; } = [];
}

public sealed class Game
{
    public int Id { get; init; }

    public Guid PlayerId { get; init; }

    public int Stake { get; init; }

    public int Payout { get; init; }

    public DateTimeOffset PlayedAt { get; init; }
}
