using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal abstract class LoggingDecoratorBase<TRequest>(ILogger logger)
    where TRequest : IRequest
{
    private static readonly string RequestName = typeof(TRequest).Name;

    protected async Task<IResult> ExecuteWithLoggingAsync(
        Func<CancellationToken, Task<IResult>> next,
        CancellationToken cancellationToken)
    {
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
                logger.LogRequestFailed(RequestName, result);
            }
            else
            {
                logger.LogRequestSucceeded(RequestName);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogRequestCanceledMidExecution(RequestName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogRequestFaulted(RequestName, ex);

            return new InternalError(ex);
        }
    }
}

internal abstract class LoggingDecoratorBase<TRequest, TResult>(ILogger logger)
    where TRequest : IRequest
    where TResult : notnull
{
    private static readonly string RequestName = typeof(TRequest).Name;

    protected async Task<IResult<TResult>> ExecuteWithLoggingAsync(
        Func<CancellationToken, Task<IResult<TResult>>> next,
        CancellationToken cancellationToken)
    {
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
                logger.LogRequestFailed(RequestName, result);
            }
            else
            {
                logger.LogRequestSucceeded(RequestName);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogRequestCanceledMidExecution(RequestName);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogRequestFaulted(RequestName, ex);

            return new InternalError<TResult>(ex);
        }
    }
}
