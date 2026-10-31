using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "'{RequestName}' started")]
    public static partial void LogRequestStarted(this ILogger logger, string requestName);

    [LoggerMessage(LogLevel.Information, "'{RequestName}' completed successfully")]
    public static partial void LogRequestSucceeded(this ILogger logger, string requestName);

    [LoggerMessage(LogLevel.Warning, "'{RequestName}' canceled before handling")]
    public static partial void LogRequestCanceledPreExecution(this ILogger logger, string requestName);

    [LoggerMessage(LogLevel.Warning, "'{RequestName}' failed: {Result}")]
    public static partial void LogRequestFailed(this ILogger logger, string requestName, IResult result);

    [LoggerMessage(LogLevel.Warning, "'{RequestName}' canceled during handling")]
    public static partial void LogRequestCanceledMidExecution(this ILogger logger, string requestName);

    [LoggerMessage(LogLevel.Error, "'{RequestName}' threw an exception")]
    public static partial void LogRequestFaulted(this ILogger logger, string requestName, Exception ex);

    [LoggerMessage(LogLevel.Warning, "'{RequestName}' failed validation: {ValidationState}")]
    public static partial void LogRequestValidationFailed(this ILogger logger, string requestName, ValidationState validationState);

    [LoggerMessage(LogLevel.Information, "'{RequestName}' canceled while persisting changes")]
    public static partial void LogSaveChangesCanceled(this ILogger logger, string requestName);

    [LoggerMessage(LogLevel.Error, "'{RequestName}' threw an unhandled exception while persisting changes")]
    public static partial void LogSaveChangesFaulted(this ILogger logger, string requestName, Exception ex);

    [LoggerMessage(LogLevel.Warning, "'{RequestName}' canceled before its post commit hook")]
    public static partial void LogPostCommitHookCanceledPreExecution(this ILogger logger, string requestName);

    [LoggerMessage(LogLevel.Warning, "'{RequestName}' canceled during its post commit hook")]
    public static partial void LogPostCommitHookCanceledMidExecution(this ILogger logger, string requestName);

    [LoggerMessage(LogLevel.Error, "'{RequestName}' threw an exception during its post commit hook")]
    public static partial void LogPostCommitFaulted(this ILogger logger, string requestName, Exception ex);

    [LoggerMessage(LogLevel.Warning, "'{RequestName}' canceled during transaction")]
    public static partial void LogTransactionCanceled(this ILogger logger, string requestName);

    [LoggerMessage(LogLevel.Warning, "'{RequestName}' threw an exception during transaction")]
    public static partial void LogTransactionFaulted(this ILogger logger, string requestName, Exception ex);
}
