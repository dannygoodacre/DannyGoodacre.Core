namespace DannyGoodacre.Core.CommandQuery.Abstractions;

/// <summary>
/// Defines an abstraction for an active transaction boundary.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    /// <summary>
    /// Commit all changes made during this transaction to the application state.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discard all changes made during this transaction.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
