using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// Defines an abstraction extending the state unit to support explicit transaction management.
/// </summary>
public interface ITransactionUnit : IStateUnit
{
    /// <summary>
    /// Execute the specified asynchronous operation within an atomic transaction boundary.
    /// </summary>
    /// <typeparam name="TResult">The type of <see cref="IResult"/> returned by the operation.</typeparam>
    /// <param name="operation">The asynchronous operation delegate to execute inside the transaction.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while executing the operation and controlling the transaction.</param>
    /// <returns>The <typeparamref name="TResult"/> produced by the operation.</returns>
    /// <remarks>
    /// Implementations are responsible for committing the transaction if the operation succeeds, or rolling back if the operation fails or throws an exception.
    /// </remarks>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
}
