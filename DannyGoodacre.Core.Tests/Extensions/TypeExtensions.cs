using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace DannyGoodacre.Core.Tests.Extensions;

[TestFixture]
public class TypeExtensionsTests
{
    public class Command : ICommand;

    public class Query : IQuery;

    public class SimpleCommandHandler(ILogger logger) : CommandHandler<Command>(logger)
    {
        protected override string CommandName => "Simple Command";

        protected override Task<Result> InternalExecuteAsync(Command command, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
    public class ResultCommandHandler(ILogger logger) : CommandHandler<Command, int>(logger)
    {
        protected override string CommandName => "Result Command";

        protected override Task<Result<int>> InternalExecuteAsync(Command command, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
    public class DeepCommandHandler(ILogger logger) : SimpleCommandHandler(logger)
    { }

    public class SimpleQueryHandler(ILogger logger) : QueryHandler<Query, int>(logger)
    {
        protected override string QueryName => "Simple Query";

        protected override Task<Result<int>> InternalExecuteAsync(Query query, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
    public class DeepQueryHandler(ILogger logger) : SimpleQueryHandler(logger)
    { }

    public class NotAHandler { }
    public class StringList : List<string> { }

    [TestCase(typeof(SimpleCommandHandler), ExpectedResult = true)]
    [TestCase(typeof(ResultCommandHandler), ExpectedResult = true)]
    [TestCase(typeof(DeepCommandHandler), ExpectedResult = true)]
    [TestCase(typeof(NotAHandler), ExpectedResult = false)]
    [TestCase(typeof(SimpleQueryHandler), ExpectedResult = false)]
    public bool IsCommandHandler_ReturnsExpectedValue(Type type)
    {
        return type.IsCommandHandler();
    }

    [TestCase(typeof(SimpleQueryHandler), ExpectedResult = true)]
    [TestCase(typeof(DeepQueryHandler), ExpectedResult = true)]
    [TestCase(typeof(SimpleCommandHandler), ExpectedResult = false)]
    [TestCase(typeof(NotAHandler), ExpectedResult = false)]
    public bool IsQueryHandler_ReturnsExpectedValue(Type type)
    {
        return type.IsQueryHandler();
    }
}
