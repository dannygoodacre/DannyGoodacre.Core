using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace DannyGoodacre.Core.Tests.Extensions;

[TestFixture]
public class TypeExtensionsTests
{
    private class Command : ICommand;

    private class Query : IQuery;

    private class SimpleCommandHandler(ILogger logger) : CommandHandler<Command>(logger)
    {
        protected override string CommandName => "Simple Command";

        protected override Task<Result> InternalExecuteAsync(Command command, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private class CommandWithValueHandler(ILogger logger) : CommandHandler<Command, int>(logger)
    {
        protected override string CommandName => "Result Command";

        protected override Task<Result<int>> InternalExecuteAsync(Command command, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private class DeepCommandHandler(ILogger logger) : SimpleCommandHandler(logger);

    private class SimpleQueryHandler(ILogger logger) : QueryHandler<Query, int>(logger)
    {
        protected override string QueryName => "Simple Query";

        protected override Task<Result<int>> InternalExecuteAsync(Query query, CancellationToken cancellationToken)
            => throw new NotImplementedException();
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
