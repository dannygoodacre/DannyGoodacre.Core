namespace DannyGoodacre.Core.CommandQuery.Abstractions;

/// <summary>
/// Provides functionality for initiating an <see cref="ITransaction"/>.
/// </summary>
public interface ITransactionProvider
{
    /// <summary>
    /// Start a new transaction.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="ITransaction"/> instance.</returns>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
