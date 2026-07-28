using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// Defines an abstraction extending the state unit to support explicit transaction management.
/// </summary>
public interface ITransactionUnit : IStateUnit
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) where TResult : Result;
}
