namespace DannyGoodacre.Core.CommandQuery.Abstractions;

/// <summary>
/// Provides functionality for coordinating and persisting changes to an underlying data store as a single atomic unit.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persist all changes made during this operation to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>The number of state entries written to the store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a new transaction to ensure multiple operations succeed or fail as a single unit.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="ITransaction"/> instance.</returns>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
