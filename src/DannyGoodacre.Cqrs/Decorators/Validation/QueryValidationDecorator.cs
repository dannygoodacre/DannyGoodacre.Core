using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal sealed class QueryValidationDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    ILogger<TQuery> logger,
    IValidator<TQuery>? validator = null)
    : ValidationDecoratorBase<TQuery, TResult>(logger, validator), IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : notnull
{
    public Task<IResult<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        => ExecuteWithValidationAsync(query, ct => inner.HandleAsync(query, ct), cancellationToken);
}
