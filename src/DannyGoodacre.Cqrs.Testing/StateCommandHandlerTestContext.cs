using DannyGoodacre.Primitives;
using Moq;

namespace DannyGoodacre.Cqrs.Testing;

/// <summary>
/// Core base class providing shared mocks and setup for state command handler test contexts.
/// </summary>
public abstract class StateCommandHandlerTestContextCore<TCommand, TInnerHandler, TStateUnit>
    : HandlerTestContext<TCommand>
    where TCommand : class, ICommand
    where TInnerHandler : class
    where TStateUnit : class, IStateUnit
{
    public Mock<TInnerHandler> InnerMock { get; } = new(MockBehavior.Strict);

    public Mock<TStateUnit> StateUnitMock { get; } = new(MockBehavior.Strict);

    /// <summary>
    /// Configures <see cref="IStateUnit.SaveChangesAsync"/> to return a successful entry count.
    /// </summary>
    public void SetupSaveChangesAsync(int entriesWritten = 1)
    {
        StateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entriesWritten);
    }
}

/// <summary>
/// Test context for building and testing void-returning state command handler pipelines.
/// </summary>
public class StateCommandHandlerTestContext<TCommand>
    : StateCommandHandlerTestContextCore<TCommand, ICommandHandler<TCommand>, IStateUnit>
    where TCommand : class, ICommand
{
    public Mock<IPostCommitHook<TCommand>> PostCommitHookMock { get; } = new(MockBehavior.Strict);

    public void SetupPostCommitHook(TCommand command, IResult expectedResult)
    {
        PostCommitHookMock
            .Setup(x => x.ExecuteAsync(command, expectedResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public ICommandHandler<TCommand> CreatePipeline(bool includePostCommitHook = false)
    {
        return new StateCommandHandlerDecorator<TCommand>(
            InnerMock.Object,
            StateUnitMock.Object,
            Logger,
            includePostCommitHook ? PostCommitHookMock.Object : null);
    }
}

/// <summary>
/// Test context for building and testing value-returning state command handler pipelines.
/// </summary>
public class StateCommandHandlerTestContext<TCommand, TResult>
    : StateCommandHandlerTestContextCore<TCommand, ICommandHandler<TCommand, TResult>, IStateUnit>
    where TCommand : class, ICommand
    where TResult : notnull
{
    public Mock<IPostCommitHook<TCommand, TResult>> PostCommitHookMock { get; } = new(MockBehavior.Strict);

    public void SetupPostCommitHook(TCommand command, IResult<TResult> expectedResult)
    {
        PostCommitHookMock
            .Setup(x => x.AfterCommitAsync(command, expectedResult, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public ICommandHandler<TCommand, TResult> CreatePipeline(bool includePostCommitHook = false)
    {
        return new StateCommandHandlerDecorator<TCommand, TResult>(
            InnerMock.Object,
            StateUnitMock.Object,
            Logger,
            includePostCommitHook ? PostCommitHookMock.Object : null);
    }
}
