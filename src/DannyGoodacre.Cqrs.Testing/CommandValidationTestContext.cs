using Moq;

namespace DannyGoodacre.Cqrs.Testing;

public class CommandValidationTestContext<TCommand>(MockBehavior mockBehavior = MockBehavior.Default)
    : TestContext<TCommand>
    where TCommand : class, ICommand
{
    public Mock<ICommandHandler<TCommand>> InnerMock { get; } = new(mockBehavior);

    public Mock<IValidator<TCommand>> ValidatorMock { get; } = new(mockBehavior);

    public ICommandHandler<TCommand> CreatePipeline(bool includeValidator = true)
        => new CommandValidationDecorator<TCommand>(
            InnerMock.Object,
            Logger,
            includeValidator ? [ValidatorMock.Object] : []);
}

public class CommandValidationTestContext<TCommand, TResult>(MockBehavior mockBehavior = MockBehavior.Default)
    : TestContext<TCommand>
    where TCommand : class, ICommand
    where TResult : notnull
{
    public Mock<ICommandHandler<TCommand, TResult>> InnerMock { get; } = new(mockBehavior);

    public Mock<IValidator<TCommand>> ValidatorMock { get; } = new(mockBehavior);

    public ICommandHandler<TCommand, TResult> CreatePipeline(bool includeValidator = true)
        => new CommandValidationDecorator<TCommand, TResult>(
            InnerMock.Object,
            Logger,
            includeValidator ? [ValidatorMock.Object] : []);
}

public class QueryValidationTestContext<TQuery, TResult>(MockBehavior mockBehavior = MockBehavior.Default)
    : TestContext<TQuery>
    where TQuery : class, IQuery<TResult>
    where TResult : notnull
{
    public Mock<IQueryHandler<TQuery, TResult>> InnerMock { get; } = new(mockBehavior);

    public Mock<IValidator<TQuery>> ValidatorMock { get; } = new(mockBehavior);

    public IQueryHandler<TQuery, TResult> CreatePipeline(bool includeValidator = true)
        => new QueryValidationDecorator<TQuery, TResult>(
            InnerMock.Object,
            Logger,
            includeValidator ? [ValidatorMock.Object] : []);
}
