using Moq;

namespace DannyGoodacre.Cqrs.Testing;

/// <summary>
/// Test context for building and testing the validation decorator of a command handler that returns no value.
/// </summary>
/// <typeparam name="TCommand">The type of command to test.</typeparam>
/// <param name="mockBehavior">The behavior of every mock that the context creates.</param>
public class CommandValidationTestContext<TCommand>(MockBehavior mockBehavior = MockBehavior.Default)
    : TestContext<TCommand>
    where TCommand : class, ICommand
{
    /// <summary>
    /// The mock of the handler that the decorator wraps.
    /// </summary>
    public Mock<ICommandHandler<TCommand>> InnerMock { get; } = new(mockBehavior);

    /// <summary>
    /// The mock of the validator that the decorator runs.
    /// </summary>
    public Mock<IValidator<TCommand>> ValidatorMock { get; } = new(mockBehavior);

    /// <summary>
    /// Creates the validation decorator around <see cref="InnerMock"/>.
    /// </summary>
    /// <param name="includeValidator">
    /// Whether the decorator runs <see cref="ValidatorMock"/>. Pass <see langword="false"/> to test a command with no validators.
    /// </param>
    /// <returns>The decorator under test.</returns>
    public ICommandHandler<TCommand> CreatePipeline(bool includeValidator = true)
        => new CommandValidationDecorator<TCommand>(
            InnerMock.Object,
            Logger,
            includeValidator ? [ValidatorMock.Object] : []);
}

/// <summary>
/// Test context for building and testing the validation decorator of a command handler that returns a value.
/// </summary>
/// <typeparam name="TCommand">The type of command to test.</typeparam>
/// <typeparam name="TResult">The type of value that the handler returns.</typeparam>
/// <param name="mockBehavior">The behavior of every mock that the context creates.</param>
public class CommandValidationTestContext<TCommand, TResult>(MockBehavior mockBehavior = MockBehavior.Default)
    : TestContext<TCommand>
    where TCommand : class, ICommand
    where TResult : notnull
{
    /// <summary>
    /// The mock of the handler that the decorator wraps.
    /// </summary>
    public Mock<ICommandHandler<TCommand, TResult>> InnerMock { get; } = new(mockBehavior);

    /// <summary>
    /// The mock of the validator that the decorator runs.
    /// </summary>
    public Mock<IValidator<TCommand>> ValidatorMock { get; } = new(mockBehavior);

    /// <summary>
    /// Creates the validation decorator around <see cref="InnerMock"/>.
    /// </summary>
    /// <param name="includeValidator">
    /// Whether the decorator runs <see cref="ValidatorMock"/>. Pass <see langword="false"/> to test a command with no validators.
    /// </param>
    /// <returns>The decorator under test.</returns>
    public ICommandHandler<TCommand, TResult> CreatePipeline(bool includeValidator = true)
        => new CommandValidationDecorator<TCommand, TResult>(
            InnerMock.Object,
            Logger,
            includeValidator ? [ValidatorMock.Object] : []);
}

/// <summary>
/// Test context for building and testing the validation decorator of a query handler.
/// </summary>
/// <typeparam name="TQuery">The type of query to test.</typeparam>
/// <typeparam name="TResult">The type of value that the query returns.</typeparam>
/// <param name="mockBehavior">The behavior of every mock that the context creates.</param>
public class QueryValidationTestContext<TQuery, TResult>(MockBehavior mockBehavior = MockBehavior.Default)
    : TestContext<TQuery>
    where TQuery : class, IQuery<TResult>
    where TResult : notnull
{
    /// <summary>
    /// The mock of the handler that the decorator wraps.
    /// </summary>
    public Mock<IQueryHandler<TQuery, TResult>> InnerMock { get; } = new(mockBehavior);

    /// <summary>
    /// The mock of the validator that the decorator runs.
    /// </summary>
    public Mock<IValidator<TQuery>> ValidatorMock { get; } = new(mockBehavior);

    /// <summary>
    /// Creates the validation decorator around <see cref="InnerMock"/>.
    /// </summary>
    /// <param name="includeValidator">
    /// Whether the decorator runs <see cref="ValidatorMock"/>. Pass <see langword="false"/> to test a query with no validators.
    /// </param>
    /// <returns>The decorator under test.</returns>
    public IQueryHandler<TQuery, TResult> CreatePipeline(bool includeValidator = true)
        => new QueryValidationDecorator<TQuery, TResult>(
            InnerMock.Object,
            Logger,
            includeValidator ? [ValidatorMock.Object] : []);
}
