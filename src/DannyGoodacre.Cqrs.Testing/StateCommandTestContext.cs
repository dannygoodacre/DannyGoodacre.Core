using DannyGoodacre.Primitives;
using Moq;

namespace DannyGoodacre.Cqrs.Testing;

/// <summary>
/// Core base class providing shared mocks and setup for state command handler test contexts.
/// </summary>
/// <typeparam name="TCommand">The type of command to test.</typeparam>
/// <typeparam name="TInnerHandler">The type of handler that the decorator wraps.</typeparam>
/// <typeparam name="TStateUnit">The persistence unit that the decorator uses.</typeparam>
/// <param name="mockBehavior">The behavior of every mock that the context creates.</param>
public abstract class StateCommandTestContextCore<TCommand, TInnerHandler, TStateUnit>(
    MockBehavior mockBehavior = MockBehavior.Default)
    : TestContext<TCommand>
    where TCommand : class, ICommand
    where TInnerHandler : class
    where TStateUnit : class, IStateUnit
{
    /// <summary>
    /// The mock of the handler that the decorator wraps.
    /// </summary>
    public Mock<TInnerHandler> InnerMock { get; } = new(mockBehavior);

    /// <summary>
    /// The mock of the persistence unit. <see cref="IStateUnit.HasActiveTransaction"/> is set up to return
    /// <see langword="false"/>.
    /// </summary>
    public Mock<TStateUnit> StateUnitMock { get; } = CreateStateUnitMock(mockBehavior);

    /// <summary>
    /// The scope that holds the post-commit hooks of child commands until the outermost commit.
    /// </summary>
    internal PostCommitScope PostCommitScope { get; } = new();

    /// <summary>
    /// Configures <see cref="IStateUnit.HasActiveTransaction"/> to show whether a transaction that the pipeline
    /// did not start is open. The default is <see langword="false"/>.
    /// </summary>
    /// <param name="hasActiveTransaction">Whether a transaction that the pipeline did not start is open.</param>
    public void SetupHasActiveTransaction(bool hasActiveTransaction)
        => StateUnitMock
            .Setup(x => x.HasActiveTransaction)
            .Returns(hasActiveTransaction);

    // A strict mock throws for a member with no setup, also for a default interface member.
    private static Mock<TStateUnit> CreateStateUnitMock(MockBehavior mockBehavior)
    {
        var mock = new Mock<TStateUnit>(mockBehavior);

        mock.Setup(x => x.HasActiveTransaction).Returns(false);

        return mock;
    }

    /// <summary>
    /// Configures <see cref="IStateUnit.SaveChangesAsync"/> to return a successful entry count.
    /// </summary>
    /// <param name="entriesWritten">The number of entries that the save reports as written.</param>
    public void SetupSaveChangesAsync(int entriesWritten = 1)
        => StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entriesWritten);
}

/// <summary>
/// Test context for building and testing void-returning state command handler pipelines.
/// </summary>
/// <typeparam name="TCommand">The type of command to test.</typeparam>
/// <param name="mockBehavior">The behavior of every mock that the context creates.</param>
public class StateCommandTestContext<TCommand>(MockBehavior mockBehavior = MockBehavior.Default)
    : StateCommandTestContextCore<TCommand, ICommandHandler<TCommand>, IStateUnit>(mockBehavior)
    where TCommand : class, ICommand
{
    /// <summary>
    /// The mock of the post-commit hook that the decorator runs when <see cref="CreatePipeline"/> includes it.
    /// </summary>
    public Mock<IPostCommitHook<TCommand>> PostCommitHookMock { get; } = new(mockBehavior);

    /// <summary>
    /// Configures <see cref="PostCommitHookMock"/> to complete when it is called with this command and result.
    /// </summary>
    /// <param name="command">The command that the hook expects.</param>
    /// <param name="expectedResult">The handler result that the hook expects.</param>
    public void SetupPostCommitHook(TCommand command, IResult expectedResult)
        => PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, expectedResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

    /// <summary>
    /// Creates the state decorator around <see cref="StateCommandTestContextCore{TCommand, TInnerHandler, TStateUnit}.InnerMock"/>.
    /// </summary>
    /// <param name="includePostCommitHook">Whether the decorator runs <see cref="PostCommitHookMock"/>.</param>
    /// <returns>The decorator under test.</returns>
    public ICommandHandler<TCommand> CreatePipeline(bool includePostCommitHook = false)
        => new StateCommandDecorator<TCommand>(
            InnerMock.Object,
            StateUnitMock.Object,
            Logger,
            PostCommitScope,
            includePostCommitHook ? [PostCommitHookMock.Object] : []);
}

/// <summary>
/// Test context for building and testing value-returning state command handler pipelines.
/// </summary>
/// <typeparam name="TCommand">The type of command to test.</typeparam>
/// <typeparam name="TResult">The type of value that the handler returns.</typeparam>
/// <param name="mockBehavior">The behavior of every mock that the context creates.</param>
public class StateCommandTestContext<TCommand, TResult>(MockBehavior mockBehavior = MockBehavior.Default)
    : StateCommandTestContextCore<TCommand, ICommandHandler<TCommand, TResult>, IStateUnit>(mockBehavior)
    where TCommand : class, ICommand
    where TResult : notnull
{
    /// <summary>
    /// The mock of the post-commit hook that the decorator runs when <see cref="CreatePipeline"/> includes it.
    /// </summary>
    public Mock<IPostCommitHook<TCommand, TResult>> PostCommitHookMock { get; } = new(mockBehavior);

    /// <summary>
    /// Configures <see cref="PostCommitHookMock"/> to complete when it is called with this command and result.
    /// </summary>
    /// <param name="command">The command that the hook expects.</param>
    /// <param name="expectedResult">The handler result that the hook expects.</param>
    public void SetupPostCommitHook(TCommand command, IResult<TResult> expectedResult)
        => PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, expectedResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

    /// <summary>
    /// Creates the state decorator around <see cref="StateCommandTestContextCore{TCommand, TInnerHandler, TStateUnit}.InnerMock"/>.
    /// </summary>
    /// <param name="includePostCommitHook">Whether the decorator runs <see cref="PostCommitHookMock"/>.</param>
    /// <returns>The decorator under test.</returns>
    public ICommandHandler<TCommand, TResult> CreatePipeline(bool includePostCommitHook = false)
        => new StateCommandDecorator<TCommand, TResult>(
            InnerMock.Object,
            StateUnitMock.Object,
            Logger,
            PostCommitScope,
            includePostCommitHook ? [PostCommitHookMock.Object] : []);
}
