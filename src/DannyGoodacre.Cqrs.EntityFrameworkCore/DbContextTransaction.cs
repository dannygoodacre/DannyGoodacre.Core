using DannyGoodacre.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DannyGoodacre.Cqrs.EntityFrameworkCore;

/// <summary>
/// Provides state persistence and transaction management using Entity Framework Core.
/// </summary>
/// <typeparam name="TDbContext">The type of the underlying <see cref="DbContext"/>.</typeparam>
/// <param name="context">The database context instance to manage.</param>
public class DbContextTransaction<TDbContext>(TDbContext context) : ITransactionUnit
    where TDbContext : DbContext
{
    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc/>
    public bool HasActiveTransaction => context.Database.CurrentTransaction is not null;

    /// <inheritdoc/>
    /// <remarks>
    /// Integrates EF Core's <see cref="IExecutionStrategy"/> to support execution resilience and retries.
    /// If an ambient transaction is active, the operation executes within it without nesting.
    /// On failure or exception, <see cref="DbContext.ChangeTracker"/> is cleared to discard stale state.
    /// </remarks>
    public async Task<TResultWrapper> ExecuteInTransactionAsync<TResultWrapper>(
        Func<CancellationToken, Task<TResultWrapper>> operation,
        CancellationToken cancellationToken = default)
        where TResultWrapper : IResult
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            state: (context, operation),
            operation: async (dbContext, state, innerCancellationToken) =>
            {
                await using IDbContextTransaction transaction =
                    await dbContext.Database.BeginTransactionAsync(innerCancellationToken);

                try
                {
                    TResultWrapper result = await state.operation(innerCancellationToken);

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
