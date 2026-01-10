namespace DannyGoodacre.Core.CommandQuery.Abstractions;

/// <summary>
/// Provides a mechanism for managing an atomic unit of work.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    /// <summary>
    /// Commit all changes made during this transaction to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discard all changes made during this transaction.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
