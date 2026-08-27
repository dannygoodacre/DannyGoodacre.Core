using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled before execution.")]
    public static partial void LogCommandCanceledBeforeExecution(this ILogger logger, string command);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled during 'AfterSaveAsync'.")]
    public static partial void LogCommandCanceledDuringAfterSave(this ILogger logger, string command);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled during execution.")]
    public static partial void LogCommandCanceledDuringExecution(this ILogger logger, string command);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled while persisting changes.")]
    public static partial void LogCommandCanceledWhilePersistingChanges(this ILogger logger, string command);

    [LoggerMessage(LogLevel.Critical, "Command '{Command}' failed.")]
    public static partial void LogCommandFailed(this ILogger logger, string command, Exception exception);

    [LoggerMessage(LogLevel.Critical, "Command '{Command}' failed during 'AfterSaveAsync'.")]
    public static partial void LogCommandFailedDuringAfterSave(this ILogger logger, string command, Exception exception);

    [LoggerMessage(LogLevel.Error, "Command '{Command}' failed validation: {ValidationState}")]
    public static partial void LogCommandFailedValidation(this ILogger logger, string command, ValidationState validationState);

    [LoggerMessage(LogLevel.Critical, "Command '{Command}' failed while persisting changes.")]
    public static partial void LogCommandFailedWhilePersistingChanges(this ILogger logger, string command, Exception exception);

    [LoggerMessage(LogLevel.Error, "Command '{Command}' attempted to persist an unexpected number of changes: Expected '{Expected}', Actual '{Actual}'.")]
    public static partial void LogCommandUnexpectedNumberOfChanges(this ILogger logger, string command, int expected, int actual);

    [LoggerMessage(LogLevel.Information, "Query '{Query}' was canceled before execution.")]
    public static partial void LogQueryCanceledBeforeExecution(this ILogger logger, string query);

    [LoggerMessage(LogLevel.Information, "Query '{Query}' was canceled during execution.")]
    public static partial void LogQueryCanceledDuringExecution(this ILogger logger, string query);

    [LoggerMessage(LogLevel.Critical, "Query '{Query}' failed.")]
    public static partial void LogQueryFailed(this ILogger logger, string query, Exception exception);

    [LoggerMessage(LogLevel.Error, "Query '{Query}' failed validation: {ValidationState}")]
    public static partial void LogQueryFailedValidation(this ILogger logger, string query, ValidationState validationState);
}
