using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : notnull
{
    Task<IResult<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
