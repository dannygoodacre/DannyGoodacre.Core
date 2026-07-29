using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;

namespace TestProject;

public class IdentityContext(DbContextOptions options) : DbContext(options), IStateUnit, ITransactionUnit
{
    public DbSet<User> Users { get; set; }

    public DbSet<Claim> Claims { get; set; }

    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);


    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        where TResult : Result
    {
        if (Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        IExecutionStrategy strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await Database.BeginTransactionAsync(cancellationToken);

            TResult result = await operation(cancellationToken);

            if (!result.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);

                return result;
            }

            await transaction.CommitAsync(cancellationToken);

            return result;

        });
    }
}

public class IdentityContextFactory : IDesignTimeDbContextFactory<IdentityContext>
{
    public IdentityContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityContext>();
        optionsBuilder.UseSqlite("Data Source=identity.db");

        return new IdentityContext(optionsBuilder.Options);
    }
}
