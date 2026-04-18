using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace DannyGoodacre.Cqrs.Extensions.Tests;

[TestFixture]
public sealed class TypeExtensionsTests
{
    private class SimpleCommandHandler(ILogger logger) : CommandHandler<ICommand>(logger)
    {
        protected override string CommandName => "Simple Command";

        protected override Task<Result> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }

    private sealed class CommandWithValueHandler(ILogger logger) : CommandHandler<ICommand, int>(logger)
    {
        protected override string CommandName => "Result Command";

        protected override Task<Result<int>> InternalExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<int>.Success(123));
    }

    private sealed class DeepCommandHandler(ILogger logger) : SimpleCommandHandler(logger);

    private class SimpleQueryHandler(ILogger logger) : QueryHandler<IQuery, int>(logger)
    {
        protected override string QueryName => "Simple Query";

        protected override Task<Result<int>> InternalExecuteAsync(IQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Result<int>.Success(123));
    }

    private sealed class DeepQueryHandler(ILogger logger) : SimpleQueryHandler(logger);

    private sealed class NotAHandler;

    [TestCase(typeof(SimpleCommandHandler), ExpectedResult = true)]
    [TestCase(typeof(CommandWithValueHandler), ExpectedResult = true)]
    [TestCase(typeof(DeepCommandHandler), ExpectedResult = true)]
    [TestCase(typeof(NotAHandler), ExpectedResult = false)]
    [TestCase(typeof(SimpleQueryHandler), ExpectedResult = false)]
    public bool IsCommandHandler_ReturnsExpectedValue(Type type) => type.IsCommandHandler();

    [TestCase(typeof(SimpleQueryHandler), ExpectedResult = true)]
    [TestCase(typeof(DeepQueryHandler), ExpectedResult = true)]
    [TestCase(typeof(SimpleCommandHandler), ExpectedResult = false)]
    [TestCase(typeof(NotAHandler), ExpectedResult = false)]
    public bool IsQueryHandler_ReturnsExpectedValue(Type type) => type.IsQueryHandler();
}
