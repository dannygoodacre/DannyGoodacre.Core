using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal sealed class QueryLoggingDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    ILogger<TQuery> logger)
    : LoggingDecoratorBase<TQuery, TResponse>(logger), IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : notnull
{
    public Task<IResult<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        => ExecuteWithLoggingAsync(query, ct => inner.HandleAsync(query, ct), cancellationToken);
}
