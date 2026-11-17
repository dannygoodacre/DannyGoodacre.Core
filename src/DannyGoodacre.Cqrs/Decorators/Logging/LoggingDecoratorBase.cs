using System.Diagnostics;
using DannyGoodacre.Results;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal abstract class LoggingDecoratorBase<TRequest>(ILogger logger)
    where TRequest : IRequest
{
    private static readonly string RequestName = TypeNameFormatter.Format(typeof(TRequest));

    protected async Task<IResult> ExecuteWithLoggingAsync(
        Func<CancellationToken, Task<IResult>> next,
        CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            logger.LogRequestStarted(RequestName);

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogRequestCanceledPreExecution(RequestName);

                return new Canceled();
            }

            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                logger.LogRequestFailed(RequestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds, result);
            }
            else
            {
                logger.LogRequestSucceeded(RequestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogRequestCanceledMidExecution(RequestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogRequestFaulted(RequestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds, ex);

            return new InternalError(ex);
        }
    }
}

internal abstract class LoggingDecoratorBase<TRequest, TResult>(ILogger logger)
    where TRequest : IRequest
    where TResult : notnull
{
    private static readonly string RequestName = TypeNameFormatter.Format(typeof(TRequest));

    protected async Task<IResult<TResult>> ExecuteWithLoggingAsync(
        Func<CancellationToken, Task<IResult<TResult>>> next,
        CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            logger.LogRequestStarted(RequestName);

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogRequestCanceledPreExecution(RequestName);

                return new Canceled<TResult>();
            }

            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                logger.LogRequestFailed(RequestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds, result);
            }
            else
            {
                logger.LogRequestSucceeded(RequestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogRequestCanceledMidExecution(RequestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogRequestFaulted(RequestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds, ex);

            return new InternalError<TResult>(ex);
        }
    }
}
