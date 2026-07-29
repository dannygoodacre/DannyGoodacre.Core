using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;
using TestProject.Queries;

namespace TestProject;

public interface ITestCommand
{
    Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default);
}

internal sealed record CommandOrchestrationTestCommand : ICommand;

internal class CommandOrchestrationTestHandler(ILogger<CommandOrchestrationTestHandler> logger,
                                               ITransactionUnit transactionUnit,
                                               IAddUser addUser,
                                               IGetUserId getUserId,
                                               IAddClaim addClaim)
    : TransactionCommandHandler<CommandOrchestrationTestCommand, string>(logger, transactionUnit), ITestCommand
{

    protected override string CommandName => "Test";

    protected async override Task<Result<string>> InternalExecuteAsync(CommandOrchestrationTestCommand command, CancellationToken cancellationToken = default)
    {

        Result addUserResult = await addUser.ExecuteAsync("Test User New2", cancellationToken);

        if (!addUserResult.IsSuccess)
        {
            return addUserResult.MapFailure<string>();
        }

        Result<int> getUserIdResult = await getUserId.ExecuteAsync("Test User New2", cancellationToken);

        if (!getUserIdResult.IsSuccess)
        {
            return getUserIdResult.MapFailure<string>();
        }

        Console.WriteLine(getUserIdResult.Value);

        Result addClaimsResult = await addClaim.ExecuteAsync("Test Claim", cancellationToken);

        if (!addClaimsResult.IsSuccess)
        {
            return addClaimsResult.MapFailure<string>();
        }

        return Success("test response");
    }

    public Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync(new CommandOrchestrationTestCommand(), cancellationToken);
}
