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
}
