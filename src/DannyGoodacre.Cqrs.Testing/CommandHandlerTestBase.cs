using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Testing;

public abstract class CommandHandlerTestBase<TCommandHandler>
    : CommandHandlerTestCore<TCommandHandler, NUnit.Framework.Result>
    where TCommandHandler : class;

public abstract class CommandHandlerTestBase<TCommandHandler, TResultType>
    : CommandHandlerTestCore<TCommandHandler, Result<TResultType>>
    where TCommandHandler : class;

public abstract class CommandHandlerTestCore<TCommandHandler, TResult>
    : TestBase
    where TCommandHandler : class
    where TResult : NUnit.Framework.Result
{
    internal CommandHandlerTestCore() { }

    protected abstract string CommandName { get; }

    protected CancellationToken TestCancellationToken;

    protected Mock<ILogger<TCommandHandler>> LoggerMock { get; private set; } = null!;

    protected TCommandHandler CommandHandler { get; set; } = null!;

    protected abstract Task<TResult> Act();

    [SetUp]
    public virtual void BaseSetUp()
    {
        TestCancellationToken = CancellationToken.None;

        LoggerMock = new Mock<ILogger<TCommandHandler>>(MockBehavior.Strict);
    }
}
