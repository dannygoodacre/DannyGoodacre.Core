using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled before execution.")]
    public static partial void LogCanceledBeforeExecution(this ILogger logger, string command);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled during 'AfterSaveAsync'.")]
    public static partial void LogCanceledDuringAfterSave(this ILogger logger, string command);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled during execution.")]
    public static partial void LogCanceledDuringExecution(this ILogger logger, string command);

    [LoggerMessage(LogLevel.Information, "Command '{Command}' was canceled while persisting changes.")]
    public static partial void LogCanceledWhilePersistingChanges(this ILogger logger, string command);

    [LoggerMessage(LogLevel.Critical, "Command '{Command}' failed.")]
    public static partial void LogFailed(this ILogger logger, Exception exception, string command);

    [LoggerMessage(LogLevel.Critical, "Command '{Command}' failed during 'AfterSaveAsync'.")]
    public static partial void LogFailedDuringAfterSave(this ILogger logger, Exception exception, string command);

    [LoggerMessage(LogLevel.Error, "Command '{Command}' failed validation: {ValidationState}")]
    public static partial void LogFailedValidation(this ILogger logger, string command, ValidationState validationState);

    [LoggerMessage(LogLevel.Critical, "Command '{Command}' failed while persisting changes.")]
    public static partial void LogFailedWhilePersistingChanges(this ILogger logger, Exception exception, string command);

    [LoggerMessage(LogLevel.Error, "Command '{Command}' attempted to persist an unexpected number of changes: Expected '{Expected}', Actual '{Actual}'.")]
    public static partial void LogUnexpectedNumberOfChanges(this ILogger logger, string command, int expected, int actual);
}
