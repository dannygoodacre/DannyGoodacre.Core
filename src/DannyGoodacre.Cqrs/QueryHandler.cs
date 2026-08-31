using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract class QueryHandler<TQuery, TResult>(ILogger logger)
    where TQuery : IQuery
{
    /// <summary>
    /// The display name of the command.
    /// </summary>
    protected abstract string QueryName { get; }

    /// <summary>
    /// The <see cref="ILogger"/> instance for structured reporting.
    /// </summary>
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
    /// <returns>A <see cref="IResult{TResult}"/> indicating the outcome of the operation.</returns>
    protected abstract Task<IResult<TResult>> InternalExecuteAsync(TQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run the query by validating first and, if successful, execute the internal logic.
    /// </summary>
    /// <param name="query">The query to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>An <see cref="IResult{TResult}"/> indicating the outcome of the operation.</returns>
    protected async Task<IResult<TResult>> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default)
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

    protected IResult<TResult> Canceled() => new Canceled<TResult>();

    protected IResult<TResult> Conflict(string error) => new Conflict<TResult>(error);

    protected IResult<TResult> DomainError(string error) => new DomainError<TResult>(error);

    protected IResult<TResult> NotFound() => new NotFound<TResult>();

    protected IResult<TResult> InternalError(Error error) => new InternalError<TResult>(error);

    protected IResult<TResult> Invalid(ValidationState validationState) => new Invalid<TResult>(validationState);

    protected IResult<TResult> Success(TResult result) => new Success<TResult>(result);
}
