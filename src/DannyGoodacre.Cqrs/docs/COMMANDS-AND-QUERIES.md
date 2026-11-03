# Commands and queries

This document tells you how to select a request marker and how to write each type of request.
The examples come from the example project. [EXAMPLES.md](EXAMPLES.md) describes it.

## Select a marker interface

| Marker | Saves data | Transaction | Post-commit hook | Use for |
| --- | --- | --- | --- | --- |
| `ICommand` | No | No | Not allowed | Actions with no database change, for example a call to an external API. |
| `IStateCommand` | Yes. The pipeline calls `SaveChangesAsync`. | No new transaction. Uses the outer transaction if one is open. | Runs after the save | One change to one aggregate. |
| `ITransactedStateCommand` | Yes. The pipeline saves before the commit. | Yes. One explicit transaction. | Runs after the commit | Workflows that run many child commands and queries as one unit. |
| `IQuery<T>` | No | No | Not available | Read operations that return data. |

Use these rules:

1. If the request returns data and does not change state, use `IQuery<T>`.
2. If the request changes data in one step, use `IStateCommand`.
3. If the request runs more than one state-changing step, use `ITransactedStateCommand`.
4. If the request changes no data in your database, use `ICommand`.

> **Note:** A type must not implement both `IQuery<T>` and `IStateCommand` or `ITransactedStateCommand`. `AddCqrsHandlers` throws `InvalidOperationException` at startup for this type.

> **Note:** If a command implements both `IStateCommand` and `ITransactedStateCommand`, the pipeline uses the `ITransactedStateCommand` behavior.

## Handler interfaces

| Request | Handler interface | Return type |
| --- | --- | --- |
| Command with no return value | `ICommandHandler<TCommand>` | `Task<IResult>` |
| Command with a return value | `ICommandHandler<TCommand, TResult>` | `Task<IResult<TResult>>` |
| Query | `IQueryHandler<TQuery, TResult>` | `Task<IResult<TResult>>` |

`TResult` must not be nullable. To show that data is missing, return `NotFound`. Do not return `null`.

`AddCqrsHandlers` finds all handlers in the assemblies that you give it. You do not register handlers yourself.

## Validators

A validator implements `IValidator<TRequest>`. The pipeline runs it before the handler, and before any transaction or database call.
If the validator adds an error, the pipeline returns `Invalid` and stops. [ARCHITECTURE.md](ARCHITECTURE.md#validation-short-circuit) gives the steps.

Know these facts about validators:

- `AddCqrsHandlers` finds and registers them. A request can have more than one validator. The pipeline runs all of them and collects all errors in one `ValidationState`.
- `Validate` is synchronous. Use it for input rules, for example required fields and formats.
- Put rules that need the database in the handler. Return `Conflict` or `DomainError` for these rules.
- A validator can use other validators. They are registered as scoped services.

The onboarding validator reuses the rules of the child command.
From [`Onboarding/OnboardCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Onboarding/OnboardCustomerCommand.cs):

```csharp
public sealed class OnboardCustomerCommandValidator(IValidator<RegisterCustomerCommand> customerValidator)
    : IValidator<OnboardCustomerCommand>
{
    public void Validate(OnboardCustomerCommand request, ValidationState validationState)
    {
        // Reuse the rules of the child command, so the outer command fails before the transaction starts.
        customerValidator.Validate(new RegisterCustomerCommand(request.Name, request.Email), validationState);

        if (string.IsNullOrWhiteSpace(request.PlanCode))
        {
            validationState.AddError(nameof(request.PlanCode), "Plan code is required.");
        }
    }
}
```

## ICommand

`ICommand` is the base interface for all commands. Use it alone for commands that do not save data to your database.

The pipeline for an `ICommand`:

- Logs the request.
- Runs the validator, if there is one.
- Runs the handler.
- Does not call `SaveChangesAsync`.
- Does not open a transaction.
- Does not run post-commit hooks. `AddCqrsHandlers` throws at startup if a hook targets a plain `ICommand`.

The handler is the same as for other commands. Only the marker is different:

```csharp
public sealed record ResendWelcomeEmailCommand(string Email, string Name) : ICommand;
```

The example project has no plain `ICommand`. Its requests all save data or read data.

## IStateCommand

`IStateCommand` marks a command that changes state.
The handler changes tracked entities. The pipeline saves them.

The state decorator does these steps:

1. It calls the handler.
2. If the result is a failure, it returns the result. It does not save.
3. It calls `IStateUnit.SaveChangesAsync`.
4. If the save fails, it returns `InternalError` or `Canceled`. It does not run the hooks.
5. It runs the post-commit hooks. If a transaction is open, it defers them to the outermost commit.
6. It returns the handler result.

Do not call `SaveChangesAsync` in an `IStateCommand` handler.

The example is `RegisterCustomerCommand`. [README.md](../README.md#quick-start) shows the command, its validator, and its handler.
The source is [`Customers/RegisterCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Customers/RegisterCustomerCommand.cs).

### Entity keys

The handler creates its result before the pipeline saves the data.
Thus, a key that the database generates during the save is not available to the handler.

The example entities have two keys:

| Key | Type | Created by | Used by |
| --- | --- | --- | --- |
| `Id` | `int` | The database, during the insert | The persistence layer only: primary keys and foreign keys. |
| `PublicId` | `Guid` | The command handler, with `Guid.NewGuid()` | Callers: command results and query parameters. |

The handler sets `PublicId` and returns it in `Success<T>`. Queries always identify an entity by its `PublicId`.

A later step in the same transaction can read the database-generated `Id`. The child save has already inserted the row.
[The onboarding example](#example-customer-onboarding) does this to link the subscription to the customer.

## IQuery<T>

`IQuery<T>` marks a read-only request that returns a value of type `T`.

The pipeline for a query:

- Logs the request.
- Runs the validator, if there is one.
- Runs the handler.
- Does not save data or open transactions.

A query handler must not change state. Read data with `AsNoTracking()` and project it to a result type with `Select()`.

From [`Customers/GetCustomerQuery.cs`](../../DannyGoodacre.Cqrs.Examples/Customers/GetCustomerQuery.cs):

```csharp
public sealed record CustomerDetails(Guid PublicId, string Name, string Email, string? PlanCode);

// A read-only query. The pipeline logs and validates it, but never saves data.
// Queries identify a customer by the public ID. The database ID never leaves the persistence layer.
public sealed record GetCustomerQuery(Guid PublicId) : IQuery<CustomerDetails>;

public sealed class GetCustomerQueryHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomerQuery, CustomerDetails>
{
    public async Task<IResult<CustomerDetails>> HandleAsync(
        GetCustomerQuery query,
        CancellationToken cancellationToken = default)
    {
        CustomerDetails? customer = await customerRepository.FindDetailsAsync(query.PublicId, cancellationToken);

        return customer is null
            ? Result<CustomerDetails>.NotFound()
            : Result<CustomerDetails>.Success(customer);
    }
}
```

The query uses the public ID. The database ID never leaves the persistence layer.
The file also contains `GetCustomerQueryValidator`. It returns `Invalid` for an empty public ID.
[PERSISTENCE.md](PERSISTENCE.md#repositories) shows the `FindDetailsAsync` projection.

## ITransactedStateCommand

`ITransactedStateCommand` marks a command that runs inside one explicit database transaction.
Use it for a workflow that calls child commands and queries.
If one step fails, all changes from all steps roll back.

The transaction decorator does these steps:

1. It calls `ITransactionUnit.ExecuteInTransactionAsync`. The transaction unit opens a transaction.
2. Inside the transaction, it calls the handler.
3. If the result is a failure, it returns the result. The transaction unit rolls back.
4. If the result is a success, it calls `SaveChangesAsync`. The transaction unit commits.
5. After the commit, it runs the deferred hooks of child commands, then its own post-commit hooks.
6. It returns the handler result.

### Example: customer onboarding

`OnboardCustomerCommand` calls one child command and one child query. Then it reads the customer's database ID and adds a subscription.
From [`Onboarding/OnboardCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Onboarding/OnboardCustomerCommand.cs):

```csharp
// A transacted state command. All steps run in one database transaction.
// If a step fails, the transaction rolls back the changes from all steps.
public sealed record OnboardCustomerCommand(string Name, string Email, string PlanCode) : ITransactedStateCommand;

public sealed class OnboardCustomerCommandHandler(
    ICommandHandler<RegisterCustomerCommand, Guid> registerCustomerHandler,
    IQueryHandler<GetPlanQuery, PlanDetails> getPlanHandler,
    ICustomerRepository customerRepository,
    ISubscriptionRepository subscriptionRepository,
    TimeProvider timeProvider)
    : ICommandHandler<OnboardCustomerCommand, Guid>
{
    public async Task<IResult<Guid>> HandleAsync(
        OnboardCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        // Step 1: a child state command. Its SaveChangesAsync call writes the customer
        // inside this command's transaction. It does not start or commit a transaction.
        IResult<Guid> registerResult = await registerCustomerHandler.HandleAsync(
            new RegisterCustomerCommand(command.Name, command.Email), cancellationToken);

        if (registerResult is not Success<Guid>(var customerPublicId))
        {
            return registerResult;
        }

        // Step 2: a child query. If the plan does not exist, the transaction rolls back the new customer.
        IResult<PlanDetails> planResult = await getPlanHandler.HandleAsync(
            new GetPlanQuery(command.PlanCode), cancellationToken);

        if (planResult is not Success<PlanDetails>(var plan))
        {
            return planResult.MapFailure<Guid>();
        }

        // Step 3: the save in step 1 inserted the customer row inside this transaction,
        // so the database has already generated the customer's Id. Read it to link the subscription.
        int customerId = await customerRepository.GetIdAsync(customerPublicId, cancellationToken);

        // Step 4: a change made by this handler. The transaction decorator saves it before the commit.
        subscriptionRepository.Add(new Subscription
        {
            PublicId = Guid.NewGuid(),
            CustomerId = customerId,
            PlanCode = plan.Code,
            StartedOn = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)
        });

        return Result<Guid>.Success(customerPublicId);
    }
}
```

The handler returns the first failure:

- The child command result has the same type, `IResult<Guid>`. The handler returns it directly.
- The child query result is an `IResult<PlanDetails>`. `MapFailure<Guid>()` converts it to the same failure for `Guid`. See [RESULT-HANDLING.md](RESULT-HANDLING.md#send-a-child-result-to-the-caller).

The container injects the full pipeline for each child handler.
Thus, each child request is also logged and validated.

### Why IStateCommand is safe inside ITransactedStateCommand

A child `IStateCommand` does not create a nested transaction. These facts make this safe:

1. **The state decorator does not open transactions.** It only calls `IStateUnit.SaveChangesAsync`.
2. **One scope, one unit, one connection.** `IStateUnit` and `ITransactionUnit` resolve to the same scoped `DbContextTransaction<TDbContext>`. That instance uses one `DbContext`. The outer transaction is open on that `DbContext`.
3. **EF Core uses the current transaction.** When a transaction is open, `SaveChangesAsync` writes inside it. It does not start or commit its own transaction.
4. **The save writes, but does not commit.** The rows go to the database inside the outer transaction. The database applies its constraints. Later steps can read the rows.
5. **Only the outermost unit commits.** The transaction decorator commits after all steps succeed. If a step fails, the rollback also removes the rows from the earlier child saves.

If a child command is also an `ITransactedStateCommand`, its transaction decorator calls `ExecuteInTransactionAsync` again.
`DbContextTransaction<TDbContext>` sees the open transaction and does not start a second one. See [PERSISTENCE.md](PERSISTENCE.md#nested-transactions).

The example tests prove this behavior on SQLite:

| Test | Proof |
| --- | --- |
| `HandleAsync_WhenAllStepsSucceed_CommitsOneTransactionAndThenSendsEmail` | One transaction starts and one commits, although the child command calls `SaveChangesAsync`. |
| `HandleAsync_WhenPlanDoesNotExist_RollsBackTheSavedCustomer` | The child command sends `INSERT INTO "Customers"`. After the rollback, the table is empty. |

[TESTING.md](TESTING.md#transacted-command-tests) shows the tests.

A post-commit hook on a child `IStateCommand` waits for the outermost commit. If the transaction rolls back, the hook does not run. See [POST-COMMIT-HOOKS.md](POST-COMMIT-HOOKS.md#hooks-on-child-commands).

## Related documents

- [EXAMPLES.md](EXAMPLES.md)
- [POST-COMMIT-HOOKS.md](POST-COMMIT-HOOKS.md)
- [RESULT-HANDLING.md](RESULT-HANDLING.md)
- [PERSISTENCE.md](PERSISTENCE.md)
- [TESTING.md](TESTING.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
