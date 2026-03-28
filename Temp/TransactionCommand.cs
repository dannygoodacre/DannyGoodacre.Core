using DannyGoodacre.Core;
using DannyGoodacre.Core.CommandQuery;
using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace Temp;

public sealed record Command : ICommand
{
    public required string TestString { get; init; }
}

public sealed class TransactionCommandHandler(ILogger logger, ITransactionUnit transactionUnit)
    : TransactionCommandHandler<Command, int>(logger, transactionUnit)
{

    protected override string CommandName => "Foo";

    protected override int ExpectedChanges => 123;

    protected override Task<Result<int>> InternalExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        ExpectedChanges = 123;

        return Task.FromResult(Result.Success(123));
    }
}
