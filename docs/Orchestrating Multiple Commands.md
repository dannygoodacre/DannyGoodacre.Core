# Orchestrating Multiple Commands

Using the [transaction command handler](./TransactionCommandHandler.md), we can orchestrate multiple commands under one transaction.

Consider the following contrived example for demonstration purposes. The `addUser` and `addClaim` commands are [state commands](./StateCommandHandler.md) and `getUserId` a [query](./QueryHandler.md).

The first command saving its changes to the context will mean the following query can access the user's database-provided ID without having to actually persist the changes to the underlying database. However, if any part of this set of commands fails then the transaction will successfully roll back all changes.

```csharp
internal class OrchestrationCommandHandler(ILogger<OrchestrationCommandHandler> logger,
                                           ITransactionUnit transactionUnit,
                                           IAddUser addUser,
                                           IGetUserId getUserId,
                                           IAddClaim addClaim)
    : TransactionCommandHandler<OrchestrationCommand>(logger, transactionUnit), ITestCommand
{
    protected override string CommandName => "Orchestrating Command";

    protected async override Task<Result> InternalExecuteAsync(CommandOrchestrationTestCommand command, CancellationToken cancellationToken = default)
    {

        Result addUserResult = await addUser.ExecuteAsync("Username", cancellationToken);

        if (!addUserResult.IsSuccess)
        {
            return addUserResult.MapFailure<string>();
        }

        Result<int> getUserIdResult = await getUserId.ExecuteAsync("Username", cancellationToken);

        if (!getUserIdResult.IsSuccess)
        {
            return getUserIdResult.MapFailure<string>();
        }

        int userId = getUserIdResult.Value;

        Result addClaimsResult = await addClaim.ExecuteAsync(userId, "Claim", cancellationToken);

        if (!addClaimsResult.IsSuccess)
        {
            return addClaimsResult.MapFailure<string>();
        }

        return Success();
    }
```
