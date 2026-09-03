using DannyGoodacre.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DannyGoodacre.Cqrs.EntityFrameworkCore;

/// <summary>
/// State persistence and transaction management using Entity Framework Core.
/// </summary>
/// <typeparam name="TDbContext">The type of the underlying <see cref="DbContext"/>.</typeparam>
/// <param name="context">The database context instance to manage.</param>
public class DbContextTransactionUnit<TDbContext>(TDbContext context) : ITransactionUnit
    where TDbContext : DbContext
{
    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Uses EF Core's <see cref="IExecutionStrategy"/> to provide execution resilience.
    /// If an existing transaction is detected, the operation runs within it without creating a nested transaction.
    /// On failure or rollback, the <see cref="DbContext.ChangeTracker"/> is cleared to discard stale entities.
    /// </remarks>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        where TResult : IResult
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            state: (context, operation),
            operation: async (_, state, innerCancellationToken) =>
            {
                await using IDbContextTransaction transaction = await state.context.Database.BeginTransactionAsync(innerCancellationToken);

                try
                {
                    TResult result = await state.operation(innerCancellationToken);

                    if (result.IsSuccess)
                    {
                        await transaction.CommitAsync(innerCancellationToken);
                    }
                    else
                    {
                        await transaction.RollbackAsync(innerCancellationToken);

                        state.context.ChangeTracker.Clear();
                    }

                    return result;
                }
                catch
                {
                    state.context.ChangeTracker.Clear();

                    throw;
                }
            },
            verifySucceeded: null,
            cancellationToken);
    }
}
