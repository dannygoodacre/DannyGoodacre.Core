using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal static partial class LoggerExtensions
{
    // Request lifecycle
    [LoggerMessage(EventId = (int)CqrsLogEvent.RequestStarted, Level = LogLevel.Information, Message = "'{RequestName}' started")]
    public static partial void LogRequestStarted(this ILogger logger, string requestName);

    [LoggerMessage(EventId = (int)CqrsLogEvent.RequestSucceeded, Level = LogLevel.Information, Message = "'{RequestName}' completed successfully")]
    public static partial void LogRequestSucceeded(this ILogger logger, string requestName);

    [LoggerMessage(EventId = (int)CqrsLogEvent.RequestCanceledPreExecution, Level = LogLevel.Warning, Message = "'{RequestName}' canceled before handling")]
    public static partial void LogRequestCanceledPreExecution(this ILogger logger, string requestName);

    [LoggerMessage(EventId = (int)CqrsLogEvent.RequestFailed, Level = LogLevel.Warning, Message = "'{RequestName}' failed: {Result}")]
    public static partial void LogRequestFailed(this ILogger logger, string requestName, IResult result);

    [LoggerMessage(EventId = (int)CqrsLogEvent.RequestCanceledMidExecution, Level = LogLevel.Warning, Message = "'{RequestName}' canceled during handling")]
    public static partial void LogRequestCanceledMidExecution(this ILogger logger, string requestName);

    [LoggerMessage(EventId = (int)CqrsLogEvent.RequestFaulted, Level = LogLevel.Error, Message = "'{RequestName}' threw an exception")]
    public static partial void LogRequestFaulted(this ILogger logger, string requestName, Exception ex);

    [LoggerMessage(EventId = (int)CqrsLogEvent.RequestValidationFailed, Level = LogLevel.Warning, Message = "'{RequestName}' failed validation: {ValidationState}")]
    public static partial void LogRequestValidationFailed(this ILogger logger, string requestName, ValidationState validationState);

    // Persistence
    [LoggerMessage(EventId = (int)CqrsLogEvent.SaveChangesCanceled, Level = LogLevel.Warning, Message = "'{RequestName}' canceled while persisting changes")]
    public static partial void LogSaveChangesCanceled(this ILogger logger, string requestName);

    [LoggerMessage(EventId = (int)CqrsLogEvent.SaveChangesFaulted, Level = LogLevel.Error, Message = "'{RequestName}' threw an unhandled exception while persisting changes")]
    public static partial void LogSaveChangesFaulted(this ILogger logger, string requestName, Exception ex);

    // Post-commit hooks
    [LoggerMessage(EventId = (int)CqrsLogEvent.PostCommitHookCanceledMidExecution, Level = LogLevel.Warning, Message = "'{RequestName}' canceled during its post commit hook")]
    public static partial void LogPostCommitHookCanceledMidExecution(this ILogger logger, string requestName);

    [LoggerMessage(EventId = (int)CqrsLogEvent.PostCommitFaulted, Level = LogLevel.Error, Message = "'{RequestName}' threw an exception during its post commit hook")]
    public static partial void LogPostCommitFaulted(this ILogger logger, string requestName, Exception ex);

    [LoggerMessage(EventId = (int)CqrsLogEvent.PostCommitHookInUnmanagedTransaction, Level = LogLevel.Warning, Message = "'{RequestName}' ran post-commit hooks inside a transaction that the pipeline did not start. The hooks ran before that transaction commits")]
    public static partial void LogPostCommitHookInUnmanagedTransaction(this ILogger logger, string requestName);

    // Transactions
    [LoggerMessage(EventId = (int)CqrsLogEvent.TransactionCanceled, Level = LogLevel.Warning, Message = "'{RequestName}' canceled during transaction")]
    public static partial void LogTransactionCanceled(this ILogger logger, string requestName);

    [LoggerMessage(EventId = (int)CqrsLogEvent.TransactionFaulted, Level = LogLevel.Error, Message = "'{RequestName}' threw an exception during transaction")]
    public static partial void LogTransactionFaulted(this ILogger logger, string requestName, Exception ex);
}
