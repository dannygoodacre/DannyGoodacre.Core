using Microsoft.EntityFrameworkCore.Storage;

namespace DannyGoodacre.Core.Identity;

public class EfUnitOfWork(IdentityContext context) : IUnitOfWorkWithTransaction
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new EfTransaction(await context.Database.BeginTransactionAsync(cancellationToken));
}

internal class EfTransaction(IDbContextTransaction transaction) : ITransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();

    public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);
}
