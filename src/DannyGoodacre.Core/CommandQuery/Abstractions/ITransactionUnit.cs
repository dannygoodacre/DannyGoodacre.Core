namespace DannyGoodacre.Core.CommandQuery.Abstractions;

/// <summary>
/// Extends the state unit to support explicit transaction management.
/// </summary>
public interface ITransactionUnit : IStateUnit
{
    /// <summary>
    /// Start a new transaction boundary to ensure multiple operations succeed or fail atomically.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="ITransaction"/> instance.</returns>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
