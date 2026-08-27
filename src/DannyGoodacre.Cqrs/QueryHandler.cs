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
    /// <returns>A <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    protected abstract Task<Result<TResult>> InternalExecuteAsync(TQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run the query by validating first and, if successful, execute the internal logic.
    /// </summary>
    /// <param name="query">The query to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    protected async Task<Result<TResult>> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default)
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

    protected Result<TResult> Success(TResult result) => Result<TResult>.Success(result);

    protected Result<TResult> Invalid(ValidationState validationState) => Result<TResult>.Invalid(validationState);

    protected Result<TResult> DomainError(string error) => Result<TResult>.DomainError(error);

    protected Result<TResult> Conflict(string error) => Result<TResult>.Conflict(error);

    protected Result<TResult> Canceled() => Result<TResult>.Canceled();

    protected Result<TResult> NotFound() => Result<TResult>.NotFound();

    protected Result<TResult> InternalError(string error) => Result<TResult>.InternalError(error);

    protected Result<TResult> InternalError(Exception exception) => Result<TResult>.InternalError(exception);
}
