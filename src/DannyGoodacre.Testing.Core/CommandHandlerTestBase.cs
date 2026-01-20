using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Testing.Core;

public abstract class CommandHandlerTestBase<TCommandHandler>
    : CommandHandlerTestCore<TCommandHandler>
    where TCommandHandler : class
{
    protected abstract Task<Result> Act();
}

public abstract class CommandHandlerTestBase<TCommandHandler, TResult>
    : CommandHandlerTestCore<TCommandHandler>
    where TCommandHandler : class
{
    protected abstract Task<Result<TResult>> Act();
}

public abstract class CommandHandlerTestCore<TCommandHandler> : TestBase
    where TCommandHandler : class
{
    internal CommandHandlerTestCore() {}

    protected abstract string CommandName { get; }

    protected readonly CancellationToken CancellationToken = CancellationToken.None;

    protected Mock<ILogger<TCommandHandler>> LoggerMock { get; private set; } = null!;

    protected TCommandHandler CommandHandler { get; set; } = null!;

    [SetUp]
    public virtual void BaseSetUp()
        => LoggerMock = new Mock<ILogger<TCommandHandler>>(MockBehavior.Strict);

    protected void SetupLogger_FailedValidation(string message)
        => LoggerMock.Setup(LogLevel.Error, $"Command '{CommandName}' failed validation: {message}");
}
