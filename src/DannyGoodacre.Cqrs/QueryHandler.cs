using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public abstract partial class QueryHandler<TQuery, TResult>(ILogger logger)
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
    /// <param name="queryRequest">The query to validate.</param>
    protected virtual void Validate(ValidationState validationState, TQuery queryRequest) { }

    /// <summary>
    /// The internal query logic.
    /// </summary>
    /// <param name="query">The valid query to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    protected abstract Task<Result<TResult>> InternalExecuteAsync(TQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Run the query by validating first and, if successful, execute the internal logic.
    /// </summary>
    /// <param name="query">The query to validate and process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>A <see cref="Result{T}"/> indicating the outcome of the operation.</returns>
    public async Task<Result<TResult>> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        var validationState = new ValidationState();

        Validate(validationState, query);

        if (validationState.HasErrors)
        {
            LogFailedValidation(Logger, QueryName, validationState);

            return Result<TResult>.Invalid(validationState);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            LogCanceledBeforeExecution(Logger, QueryName);

            return Result<TResult>.Canceled();
        }

        try
        {
            return await InternalExecuteAsync(query, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogCanceledDuringExecution(Logger, QueryName);

            return Result<TResult>.Canceled();
        }
        catch (Exception ex)
        {
            LogCriticalFailure(Logger, ex, QueryName);

            return Result<TResult>.InternalError(ex.Message);
        }
    }

    [LoggerMessage(LogLevel.Error, "Query '{Query}' failed validation: {ValidationState}")]
    private static partial void LogFailedValidation(ILogger logger, string query, ValidationState validationState);

    [LoggerMessage(LogLevel.Information, "Query '{Query}' was canceled before execution.")]
    private static partial void LogCanceledBeforeExecution(ILogger logger, string query);

    [LoggerMessage(LogLevel.Information, "Query '{Query}' was canceled during execution.")]
    private static partial void LogCanceledDuringExecution(ILogger logger, string query);

    [LoggerMessage(LogLevel.Critical, "Query '{Query}' failed.")]
    private static partial void LogCriticalFailure(ILogger logger, Exception exception, string query);
}
