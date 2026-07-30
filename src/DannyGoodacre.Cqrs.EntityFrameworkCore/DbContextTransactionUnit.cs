using DannyGoodacre.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DannyGoodacre.Cqrs.EntityFrameworkCore;

public class DbContextTransactionUnit<TDbContext>(TDbContext context) : IStateUnit, ITransactionUnit
    where TDbContext : DbContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        where TResult : Result
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            state: (context, operation),
            operation: async (_, state, ct) =>
            {
                await using IDbContextTransaction transaction = await state.context.Database.BeginTransactionAsync(ct);

                try
                {
                    TResult result = await state.operation(ct);

                    if (!result.IsSuccess)
                    {
                        await transaction.RollbackAsync(ct);

                        return result;
                    }

                    await transaction.CommitAsync(ct);

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(ct);

                    state.context.ChangeTracker.Clear();

                    throw;
                }
            },
            verifySucceeded: null,
            cancellationToken);
    }
}
