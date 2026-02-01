namespace DannyGoodacre.Core.CommandQuery.Abstractions;
/// <summary>
/// Provides functionality for persisting changes to an underlying data store.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persist all changes made during this operation to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>The number of state entries written to the store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
