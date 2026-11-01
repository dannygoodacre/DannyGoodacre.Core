using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal abstract class LoggingDecoratorBase<TRequest>(ILogger logger)
    where TRequest : IRequest
{
    protected async Task<IResult> ExecuteWithLoggingAsync(
        Func<CancellationToken, Task<IResult>> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        try
        {
            logger.LogRequestStarted(requestName);

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogRequestCanceledPreExecution(requestName);

                return new Canceled();
            }

            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                logger.LogRequestFailed(requestName, result);
            }
            else
            {
                logger.LogRequestSucceeded(requestName);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogRequestCanceledMidExecution(requestName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogRequestFaulted(requestName, ex);

            return new InternalError(ex);
        }
    }
}

internal abstract class LoggingDecoratorBase<TRequest, TResult>(ILogger logger)
    where TRequest : IRequest
    where TResult : notnull
{
    protected async Task<IResult<TResult>> ExecuteWithLoggingAsync(
        Func<CancellationToken, Task<IResult<TResult>>> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        try
        {
            logger.LogRequestStarted(requestName);

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogRequestCanceledPreExecution(requestName);

                return new Canceled<TResult>();
            }

            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                logger.LogRequestFailed(requestName, result);
            }
            else
            {
                logger.LogRequestSucceeded(requestName);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogRequestCanceledMidExecution(requestName);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogRequestFaulted(requestName, ex);

            return new InternalError<TResult>(ex);
        }
    }
}
