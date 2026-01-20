using DannyGoodacre.Core;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Testing.Core;

public abstract class CommandHandlerTestBase<TCommandHandler> : TestBase
    where TCommandHandler : class
{
    protected abstract string CommandName { get; }

    protected abstract Task<Result> Act();

    protected readonly CancellationToken CancellationToken =  CancellationToken.None;

    protected Mock<ILogger<TCommandHandler>> LoggerMock { get; private set; } = null!;

    protected TCommandHandler CommandHandler { get; set; } = null!;

    [SetUp]
    public void BaseSetUp()
        => LoggerMock = new Mock<ILogger<TCommandHandler>>(MockBehavior.Strict);

    protected void SetupLogger_FailedValidation(string message)
        => LoggerMock.Setup(LogLevel.Error, $"Command '{CommandName}' failed validation: {message}");
}
