using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Data;

internal static class DbContextExtensions
{
    /// <summary>
    /// Runs <paramref name="operation"/> in a transaction inside the context's execution strategy, and commits only
    /// when <paramref name="shouldCommit"/> returns <see langword="true"/>. A retry replays the whole operation.
    /// </summary>
    public static async Task<T> ExecuteInTransactionAsync<T>(
        this DbContext context,
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> shouldCommit,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        var isRetry = false;

        return await strategy.ExecuteAsync(async token =>
        {
            if (isRetry)
            {
                context.ChangeTracker.Clear();
            }

            isRetry = true;

            await using var transaction = await context.Database.BeginTransactionAsync(token);

            T result = await operation(token);

            if (shouldCommit(result))
            {
                await transaction.CommitAsync(token);
            }
            else
            {
                await transaction.RollbackAsync(token);
                context.ChangeTracker.Clear();
            }

            return result;
        }, cancellationToken);
    }
}
