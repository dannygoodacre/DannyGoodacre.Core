using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cqrs.Test;

class Program : TestBase
{
    public async static Task Main(string[] args)
    {
        IResult<int> result = new Success<int>(123);

        AssertSuccess(result, 123);

        var loggerFactory = new NullLoggerFactory();

        var logger = loggerFactory.CreateLogger<TestCommandHandler>();

        var commandHandler = new TestCommandHandler(logger);

        IResult result1 = await commandHandler.ExecuteAsync(123, CancellationToken.None);

        Console.WriteLine(result1);

        var valueCommandHandler = new TestCommandWithValueHandler(logger);

        IResult<int> result2 = await valueCommandHandler.ExecuteAsync(123, CancellationToken.None);

        Console.WriteLine(result2);
    }
}

record TestCommand(int Id) : ICommand;

class TestCommandHandler(ILogger logger) : CommandHandler<TestCommand>(logger)
{
    protected override string CommandName => "Test Command";

    protected override Task<IResult> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success());
    }

    public Task<IResult> ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => base.ExecuteAsync(new TestCommand(id), cancellationToken);
}

class TestCommandWithValueHandler(ILogger logger) : CommandHandler<TestCommand, int>(logger)
{
    protected override string CommandName => "Test Command";

    protected override Task<IResult<int>> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success(456));
    }

    public Task<IResult<int>> ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => base.ExecuteAsync(new TestCommand(id), cancellationToken);
}
