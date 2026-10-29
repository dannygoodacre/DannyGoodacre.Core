using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal abstract class LoggingDecoratorBase<TRequest>(ILogger logger)
    where TRequest : IRequest
{
    protected async Task<IResult> ExecuteWithLoggingAsync(
        TRequest request,
        Func<CancellationToken, Task<IResult>> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        try
        {
            logger.LogInformation("Processing {RequestName}", requestName);

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("{RequestName} was canceled before execution", requestName);

                return new Canceled();
            }

            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                logger.LogWarning("{RequestName} completed with failure result: {Result}", requestName, result);
            }
            else
            {
                logger.LogInformation("{RequestName} completed successfully", requestName);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("{RequestName} was canceled during execution", requestName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{RequestName} threw an unhandled exception", requestName);

            return new InternalError(ex);
        }
    }
}

internal abstract class LoggingDecoratorBase<TRequest, TResult>(ILogger logger)
    where TRequest : IRequest
    where TResult : notnull
{
    protected async Task<IResult<TResult>> ExecuteWithLoggingAsync(
        TRequest request,
        Func<CancellationToken, Task<IResult<TResult>>> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        try
        {
            logger.LogInformation("Processing {RequestName}", requestName);

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("{RequestName} was canceled before execution", requestName);

                return new Canceled<TResult>();
            }

            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                logger.LogWarning("{RequestName} completed with failure result: {Result}", requestName, result);
            }
            else
            {
                logger.LogInformation("{RequestName} completed successfully", requestName);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("{RequestName} was canceled during execution", requestName);

            return new Canceled<TResult>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{RequestName} threw an unhandled exception", requestName);

            return new InternalError<TResult>(ex);
        }
    }
}
