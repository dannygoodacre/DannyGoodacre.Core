using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Temp;

public class TransactionUnit<TContext>(TContext context) : ITransactionUnit
    where TContext : DbContext
{

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new Transaction(await context.Database.BeginTransactionAsync(cancellationToken));
}
