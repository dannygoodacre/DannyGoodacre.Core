namespace DannyGoodacre.Core.CommandQuery.Abstractions;

/// <summary>
/// Provides functionality for coordinating and persisting changes to an underlying data store as a single atomic unit.
/// </summary>
public interface ITransactionalUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Start a new transaction to ensure multiple operations succeed or fail as a single unit.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="ITransaction"/> instance.</returns>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
