using DannyGoodacre.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace TestProject;

public class IdentityContext(DbContextOptions options) : DbContext(options), IStateUnit, ITransactionUnit
{
    public DbSet<User> Users { get; set; }

    public DbSet<Claim> Claims { get; set; }

    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is not null)
        {
            return new NoOpTransaction();
        }

        return new Transaction(await Database.BeginTransactionAsync(cancellationToken));
    }
}
