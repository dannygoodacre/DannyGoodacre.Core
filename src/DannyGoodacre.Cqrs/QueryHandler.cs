using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract class QueryHandler<TQuery, TResultType>(ILogger logger)
    where TQuery : IQuery
{
    protected abstract string QueryName { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Validate the query before execution.
    /// </summary>
    /// <param name="validationState">A <see cref="ValidationState"/> to populate with the operation's outcome.</param>
    /// <param name="query">The query to validate.</param>
    protected virtual void Validate(ValidationState validationState, TQuery query) { }

    /// <summary>
    /// The internal query logic.
    /// </summary>
    /// <param name="query">The valid query to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="IResult{TResult}"/> indicating the outcome of the operation.</returns>
    protected abstract Task<IResult<TResultType>> InternalExecuteAsync(TQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run the query by validating first and, if successful, execute the internal logic.
    /// </summary>
    /// <param name="query">The query to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="IResult{TResult}"/> indicating the outcome of the operation.</returns>
    protected async Task<IResult<TResultType>> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationState = new ValidationState();

            Validate(validationState, query);

            if (validationState.HasErrors)
            {
                Logger.LogQueryFailedValidation(QueryName, validationState);

                return Invalid(validationState);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                return await InternalExecuteAsync(query, cancellationToken);
            }

            Logger.LogQueryCanceledBeforeExecution(QueryName);

            return Canceled();

        }
        catch (OperationCanceledException)
        {
            Logger.LogQueryCanceledDuringExecution(QueryName);

            return Canceled();
        }
        catch (Exception ex)
        {
            Logger.LogQueryFailed(QueryName, ex);

            return InternalError(ex.Message);
        }
    }

    protected IResult<TResultType> Canceled() => new Canceled<TResultType>();

    protected IResult<TResultType> Conflict(string message) => new Conflict<TResultType>(message);

    protected IResult<TResultType> DomainError(string message) => new DomainError<TResultType>(message);

    protected IResult<TResultType> NotFound() => new NotFound<TResultType>();

    protected IResult<TResultType> InternalError(Error error) => new InternalError<TResultType>(error);

    protected IResult<TResultType> Invalid(ValidationState validationState) => new Invalid<TResultType>(validationState);

    protected IResult<TResultType> Success(TResultType value) => new Success<TResultType>(value);
}
