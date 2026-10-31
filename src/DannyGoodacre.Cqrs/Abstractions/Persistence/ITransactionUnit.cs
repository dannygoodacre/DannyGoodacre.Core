using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// Defines a contract extending <see cref="IStateUnit"/> to support explicit, atomic transaction management.
/// </summary>
public interface ITransactionUnit : IStateUnit
{
    /// <summary>
    /// Executes the specified asynchronous operation within an atomic transaction boundary.
    /// </summary>
    /// <typeparam name="TResultWrapper">The type of <see cref="IResult"/> returned by the operation.</typeparam>
    /// <param name="operation">The asynchronous operation delegate to execute inside the transaction.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during transaction execution.</param>
    /// <returns>The <typeparamref name="TResultWrapper"/> produced by the operation.</returns>
    /// <remarks>
    /// Implementations are responsible for committing the transaction if the operation succeeds
    /// (<see cref="IResult.IsSuccess"/>), or rolling back if the operation returns a failure result or throws an exception.
    /// </remarks>
    Task<TResultWrapper> ExecuteInTransactionAsync<TResultWrapper>(
        Func<CancellationToken, Task<TResultWrapper>> operation,
        CancellationToken cancellationToken = default)
        where TResultWrapper : IResult;
}
