using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace TestProject;

public interface ITest
{
    Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default);
}

internal sealed record CommandOrchestrationTestCommand : ICommand;

internal class CommandOrchestrationTestHandler(ILogger<CommandOrchestrationTestHandler> logger,
                                               ITransactionUnit transactionUnit,
                                               IFirstCommand firstCommand,
                                               ISecondCommand secondCommand)
    : TransactionCommandHandler<CommandOrchestrationTestCommand, string>(logger, transactionUnit), ITest
{

    protected override string CommandName => "Test";

    protected async override Task<Result<string>> InternalExecuteAsync(CommandOrchestrationTestCommand command, CancellationToken cancellationToken = default)
    {

        Result<int> addUserResult = await firstCommand.ExecuteAsync("foo", "bar", cancellationToken);

        if (!addUserResult.IsSuccess)
        {
            return addUserResult.MapFailure<string>();
        }

        Result addClaimsResult = await secondCommand.ExecuteAsync(addUserResult.Value, cancellationToken);

        if (!addClaimsResult.IsSuccess)
        {
            return addClaimsResult.MapFailure<string>();
        }

        return Success("test response");
    }

    public Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync(new CommandOrchestrationTestCommand(), cancellationToken);
}
