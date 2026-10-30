using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// Defines a read-only query handler that executes an <see cref="IQuery{TResult}"/> and returns an <see cref="IResult{TResult}"/>.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle.</typeparam>
/// <typeparam name="TResult">The type of data returned by the query.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : notnull
{
    /// <summary>
    /// Handles the query asynchronously.
    /// </summary>
    /// <param name="query">The query instance to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="IResult{TResult}"/> containing the query result payload or error state.</returns>
    Task<IResult<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
