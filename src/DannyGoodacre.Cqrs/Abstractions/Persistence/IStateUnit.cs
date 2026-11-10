namespace DannyGoodacre.Cqrs;

/// <summary>
/// Defines a unit of work contract for persisting accumulated state changes to the underlying data store.
/// </summary>
public interface IStateUnit
{
    /// <summary>
    /// Asynchronously flushes and persists all pending state changes tracked within the current scope
    /// to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests while persisting state changes.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous save operation. The task result contains the total number
    /// of state entries written to the store.
    /// </returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether a database transaction is open on this unit.
    /// </summary>
    /// <remarks>
    /// The pipeline uses this value to detect a transaction that it did not start, for example a transaction
    /// that the application opened by hand. Post-commit hooks cannot wait for the commit of such a transaction.
    /// The default implementation returns <see langword="false"/>, so existing implementations do not change.
    /// </remarks>
    bool HasActiveTransaction => false;
}
