using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace Cqrs.Test;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}

record TestCommand : ICommand
{
    public int Id { get; init; }
}

class TestCommandHandler(ILogger logger) : CommandHandler<TestCommand>(logger)
{
    protected override string CommandName => "Test Command";

    protected override Task<IResult> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success());
    }
}

class TestCommandWithValueHandler(ILogger logger) : CommandHandler<TestCommand, int>(logger)
{
    protected override string CommandName => "Test Command";

    protected override Task<IResult<int>> InternalExecuteAsync(TestCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success(123));
    }
}
