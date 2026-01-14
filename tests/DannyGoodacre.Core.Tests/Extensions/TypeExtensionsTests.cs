using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using DannyGoodacre.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.Tests.Extensions;

[TestFixture]
public class TypeExtensionsTests
{
    private class CommandRequest : ICommandRequest;

    private class QueryRequest : IQueryRequest;

    private class SimpleCommandHandler(ILogger logger) : CommandHandler<CommandRequest>(logger)
    {
        protected override string CommandName => "Simple Command";

        protected override Task<Result> InternalExecuteAsync(CommandRequest commandRequest, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());
    }

    private class CommandWithValueHandler(ILogger logger) : CommandHandler<CommandRequest, int>(logger)
    {
        protected override string CommandName => "Result Command";

        protected override Task<Result<int>> InternalExecuteAsync(CommandRequest commandRequest, CancellationToken cancellationToken)
            => Task.FromResult(Result<int>.Success(123));
    }

    private class DeepCommandHandler(ILogger logger) : SimpleCommandHandler(logger);

    private class SimpleQueryHandler(ILogger logger) : QueryHandler<QueryRequest, int>(logger)
    {
        protected override string QueryName => "Simple Query";

        protected override Task<Result<int>> InternalExecuteAsync(QueryRequest queryRequest, CancellationToken cancellationToken)
            => Task.FromResult(Result<int>.Success(123));
    }

    private class DeepQueryHandler(ILogger logger) : SimpleQueryHandler(logger);

    private class NotAHandler;

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
