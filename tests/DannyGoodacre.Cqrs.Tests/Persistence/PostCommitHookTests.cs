using Microsoft.Extensions.Logging.Abstractions;

namespace DannyGoodacre.Cqrs.Tests;

/// <summary>
/// Tests a transacted parent command that calls a child state command, with the real decorators.
/// </summary>
public sealed class PostCommitHookTests
{
    public sealed class ChildCommand : IStateCommand;

    public sealed class ParentCommand : ITransactedStateCommand;

    private readonly List<string> _events = [];

    private readonly PostCommitScope _postCommitScope = new();

    private readonly Mock<ITransactionUnit> _transactionUnitMock = new(MockBehavior.Strict);

    private readonly Mock<ICommandHandler<ChildCommand>> _childHandlerMock = new(MockBehavior.Strict);

    private readonly Mock<ICommandHandler<ParentCommand>> _parentHandlerMock = new(MockBehavior.Strict);

    private readonly Mock<IPostCommitHook<ChildCommand>> _childHookMock = new(MockBehavior.Strict);

    private readonly Mock<IPostCommitHook<ParentCommand>> _parentHookMock = new(MockBehavior.Strict);

    public PostCommitHookTests()
    {
        _transactionUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _events.Add("save"))
            .ReturnsAsync(1);

        _childHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<ChildCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _childHookMock
            .Setup(x => x.AfterCommitAsync(It.IsAny<ChildCommand>(), It.IsAny<IResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => _events.Add("child hook"))
            .Returns(Task.CompletedTask);

        _parentHookMock
            .Setup(x => x.AfterCommitAsync(It.IsAny<ParentCommand>(), It.IsAny<IResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => _events.Add("parent hook"))
            .Returns(Task.CompletedTask);
    }

    private ICommandHandler<ChildCommand> CreateChildPipeline(params IPostCommitHook<ChildCommand>[] hooks)
        => new StateCommandDecorator<ChildCommand>(
            _childHandlerMock.Object,
            _transactionUnitMock.Object,
            NullLogger<ChildCommand>.Instance,
            _postCommitScope,
            hooks);

    private ICommandHandler<ParentCommand> CreateParentPipeline()
        => new TransactedStateCommandDecorator<ParentCommand>(
            _parentHandlerMock.Object,
            _transactionUnitMock.Object,
            NullLogger<ParentCommand>.Instance,
            _postCommitScope,
            [_parentHookMock.Object]);

    private void SetupParentHandler(IResult parentResult)
    {
        ICommandHandler<ChildCommand> childPipeline = CreateChildPipeline(_childHookMock.Object);

        _parentHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<ParentCommand>(), It.IsAny<CancellationToken>()))
            .Returns(async (ParentCommand _, CancellationToken ct) =>
            {
                (await childPipeline.HandleAsync(new ChildCommand(), ct)).ShouldBeSuccess();

                return parentResult;
            });
    }

    private void SetupTransaction(int attempts = 1)
        => _transactionUnitMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<IResult>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<CancellationToken, Task<IResult>> operation, CancellationToken ct) =>
            {
                IResult result = Result.Success();

                // More than one attempt simulates an execution strategy retry.
                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    result = await operation(ct);
                }

                _events.Add(result.IsSuccess ? "commit" : "rollback");

                return result;
            });

    [Fact]
    public async Task HandleAsync_WhenChildCommandSucceedsInsideTransaction_RunsChildHookAfterTheOuterCommit()
    {
        // Arrange
        SetupParentHandler(Result.Success());
        SetupTransaction();

        // Act
        IResult result = await CreateParentPipeline().HandleAsync(new ParentCommand(), CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        Assert.Equal(["save", "save", "commit", "child hook", "parent hook"], _events);
    }

    [Fact]
    public async Task HandleAsync_WhenTransactionRollsBackAfterChildSave_DoesNotRunChildHook()
    {
        // Arrange
        SetupParentHandler(Result.DomainError("A later step failed."));
        SetupTransaction();

        // Act
        IResult result = await CreateParentPipeline().HandleAsync(new ParentCommand(), CancellationToken.None);

        // Assert
        result.ShouldBeDomainError("A later step failed.");

        Assert.Equal(["save", "rollback"], _events);
    }

    [Fact]
    public async Task HandleAsync_WhenExecutionStrategyRetries_RunsChildHookOneTime()
    {
        // Arrange
        SetupParentHandler(Result.Success());
        SetupTransaction(attempts: 2);

        // Act
        IResult result = await CreateParentPipeline().HandleAsync(new ParentCommand(), CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        Assert.Single(_events, e => e == "child hook");
        Assert.Single(_events, e => e == "parent hook");
    }

    [Fact]
    public async Task HandleAsync_WhenStateCommandRunsWithoutTransaction_RunsHookAfterTheSave()
    {
        // Act
        IResult result = await CreateChildPipeline(_childHookMock.Object).HandleAsync(new ChildCommand(), CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        Assert.Equal(["save", "child hook"], _events);
    }

    [Fact]
    public async Task HandleAsync_WhenOneOfManyHooksThrows_RunsTheOtherHooksAndReturnsOriginalResult()
    {
        // Arrange
        var failingHookMock = new Mock<IPostCommitHook<ChildCommand>>(MockBehavior.Strict);

        failingHookMock
            .Setup(x => x.AfterCommitAsync(It.IsAny<ChildCommand>(), It.IsAny<IResult>(), It.IsAny<CancellationToken>()))
            .Callback(() => _events.Add("failing hook"))
            .ThrowsAsync(new InvalidOperationException("The email server is not available."));

        ICommandHandler<ChildCommand> pipeline = CreateChildPipeline(failingHookMock.Object, _childHookMock.Object);

        // Act
        IResult result = await pipeline.HandleAsync(new ChildCommand(), CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        Assert.Equal(["save", "failing hook", "child hook"], _events);
    }
}
