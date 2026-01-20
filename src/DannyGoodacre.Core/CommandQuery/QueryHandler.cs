using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract class QueryHandler<TQueryRequest, TResult>(ILogger logger) where TQueryRequest : IQueryRequest
{
    protected abstract string QueryName { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Validate the query before execution.
    /// </summary>
    /// <param name="validationState">A <see cref="ValidationState"/> to populate with the operation's outcome.</param>
    /// <param name="queryRequest">The query request to validate.</param>
    protected virtual void Validate(ValidationState validationState, TQueryRequest queryRequest)
    {
    }

    /// <summary>
    /// The internal query logic.
    /// </summary>
    /// <param name="query">The valid query request to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    protected abstract Task<Result<TResult>> InternalExecuteAsync(TQueryRequest query, CancellationToken cancellationToken);

    /// <summary>
    /// Run the query by validating first and, if successful, execute the internal logic.
    /// </summary>
    /// <param name="query">The query request to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    protected async Task<Result<TResult>> ExecuteAsync(TQueryRequest query, CancellationToken cancellationToken)
    {
        var validationState = new ValidationState();

        Validate(validationState, query);

        if (validationState.HasErrors)
        {
            Logger.LogError("Query '{Query}' failed validation: {ValidationState}", QueryName, validationState);

            return Result<TResult>.Invalid(validationState);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Logger.LogInformation("Query '{Query}' was cancelled before execution.", QueryName);

            return Result<TResult>.Cancelled();
        }

        try
        {
            return await InternalExecuteAsync(query, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Query '{Query}' was cancelled during execution.", QueryName);

            return Result<TResult>.Cancelled();
        }
        catch (Exception e)
        {
            Logger.LogCritical(e, "Query '{Query}' failed with exception: {Exception}", QueryName, e.Message);

            return Result<TResult>.InternalError(e.Message);
        }
    }
}
