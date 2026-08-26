using DannyGoodacre.Primitives;
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

        public void LogCanceledBeforeExecution(string commandName)
            => loggerMock.Setup(LogLevel.Information, $"Command '{commandName}' was canceled before execution.");

        public void LogCanceledDuringAfterSave(string commandName)
            => loggerMock.Setup(LogLevel.Information, $"Command '{commandName}' was canceled during 'AfterSaveAsync'.");

        public void LogCanceledDuringExecution(string commandName)
            => loggerMock.Setup(LogLevel.Information, $"Command '{commandName}' was canceled during execution.");

        public void LogCanceledWhilePersistingChanges(string commandName)
            => loggerMock.Setup(LogLevel.Information, $"Command '{commandName}' was canceled while persisting changes.");

        public void LogFailed(string commandName, Exception exception)
            => loggerMock.Setup(LogLevel.Critical, $"Command '{commandName}' failed.", exception: exception);

        public void LogFailedDuringAfterSave(string commandName, Exception exception)
            => loggerMock.Setup(LogLevel.Critical, $"Command '{commandName}' failed during 'AfterSaveAsync'.", exception: exception);

        public void LogFailedValidation(string commandName, string validationState)
            => loggerMock.Setup(LogLevel.Error, $"Command '{commandName}' failed validation: {validationState}");

        public void LogFailedWhilePersistingChanges(string commandName, Exception exception)
            => loggerMock.Setup(LogLevel.Critical, $"Command '{commandName}' failed while persisting changes.", exception: exception);

        public void LogUnexpectedNumberOfChanges(string commandName, int expected, int actual)
            => loggerMock.Setup(LogLevel.Error, $"Command '{commandName}' attempted to persist an unexpected number of changes: Expected '{expected}', Actual '{actual}'.");
    }
}
