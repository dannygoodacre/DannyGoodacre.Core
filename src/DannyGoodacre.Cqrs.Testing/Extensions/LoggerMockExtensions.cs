using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;
using Moq;

namespace DannyGoodacre.Cqrs.Testing;

public static class LoggerMockExtensions
{
    extension<T>(Mock<ILogger<T>> loggerMock)
    {
        public void IsEnabled()
            => loggerMock
                .Setup(x => x.IsEnabled(
                    It.IsAny<LogLevel>()))
                .Returns(true);

        public void LogCommandCanceledBeforeExecution(string command)
            => loggerMock.Setup(LogLevel.Information, $"Command '{command}' was canceled before execution.");

        public void LogCommandCanceledDuringAfterSave(string command)
            => loggerMock.Setup(LogLevel.Information, $"Command '{command}' was canceled during 'AfterSaveAsync'.");

        public void LogCommandCanceledDuringExecution(string command)
            => loggerMock.Setup(LogLevel.Information, $"Command '{command}' was canceled during execution.");

        public void LogCommandCanceledWhilePersistingChanges(string command)
            => loggerMock.Setup(LogLevel.Information, $"Command '{command}' was canceled while persisting changes.");

        public void LogCommandFailed(string command, Exception exception)
            => loggerMock.Setup(LogLevel.Critical, $"Command '{command}' failed.", exception: exception);

        public void LogCommandFailedDuringAfterSave(string command, Exception exception)
            => loggerMock.Setup(LogLevel.Critical, $"Command '{command}' failed during 'AfterSaveAsync'.", exception: exception);

        public void LogCommandFailedValidation(string command, string message)
            => loggerMock.Setup(LogLevel.Error, $"Command '{command}' failed validation: {message}");

        public void LogCommandFailedWhilePersistingChanges(string command, Exception exception)
            => loggerMock.Setup(LogLevel.Critical, $"Command '{command}' failed while persisting changes.", exception: exception);

        public void LogCommandUnexpectedNumberOfChanges(string command, int expected, int actual)
            => loggerMock.Setup(LogLevel.Error, $"Command '{command}' attempted to persist an unexpected number of changes: Expected '{expected}', Actual '{actual}'.");

        public void LogQueryFailedValidation(string query, string message)
            => loggerMock.Setup(LogLevel.Error, $"Query '{query}' failed validation: {message}");

        public void LogQueryCanceledBeforeExecution(string query)
            => loggerMock.Setup(LogLevel.Information, $"Query '{query}' was canceled before execution.");

        public void LogQueryCanceledDuringExecution(string query)
            => loggerMock.Setup(LogLevel.Information, $"Query '{query}' was canceled during execution.");

        public void LogQueryFailed(string query, Exception exception)
            => loggerMock.Setup(LogLevel.Critical, $"Query '{query}' failed.", exception: exception);
    }
}
