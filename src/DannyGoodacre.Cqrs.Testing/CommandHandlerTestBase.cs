using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Testing;

public abstract class CommandHandlerTestBase<TCommandHandler>
    : CommandHandlerTestCore<TCommandHandler, IResult>
    where TCommandHandler : class;

public abstract class CommandHandlerTestBase<TCommandHandler, TResult>
    : CommandHandlerTestCore<TCommandHandler, IResult<TResult>>
    where TCommandHandler : class;

public abstract class CommandHandlerTestCore<TCommandHandler, TResultWrapper>
    : TestBase
    where TCommandHandler : class
    where TResultWrapper : IResult
{
    internal CommandHandlerTestCore() { }

    protected abstract string CommandName { get; }

    protected CancellationToken TestCancellationToken;

    protected Mock<ILogger<TCommandHandler>> LoggerMock { get; private set; } = null!;

    protected TCommandHandler CommandHandler { get; set; } = null!;

    protected abstract Task<TResultWrapper> Act();

    [SetUp]
    public virtual void BaseSetUp()
    {
        TestCancellationToken = CancellationToken.None;

        LoggerMock = new Mock<ILogger<TCommandHandler>>(MockBehavior.Strict);
    }
}
