using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Testing.Core;

public static class LoggerExtensions
{
    public static void Setup<T>(this Mock<ILogger<T>> loggerMock,
        LogLevel level,
        string message,
        bool verifyContainsMessage = false,
        Exception? exception = null,
        bool verifyContainsExceptionMessage = false,
        int times = 1)
    {
        Func<object, Type, bool> state = verifyContainsMessage
            ? (v, _) => v.ToString()!.Contains(message)
            : (v, _) => v.ToString() == message;

        loggerMock
            .Setup(x => x.Log(
                It.Is<LogLevel>(y => y == level),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => state(v, t)),
                It.Is<Exception?>(exception == null
                    ? y => y == null
                    : y => y != null
                        && (verifyContainsExceptionMessage
                            ? y.Message.Contains(exception.Message)
                            : y.Message == exception.Message)),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            ))
            .Verifiable(Times.Exactly(times));
    }
}
