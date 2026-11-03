# Testing

This document shows how to test requests that use DannyGoodacre.Cqrs.
The examples come from the example test project, [`tests/DannyGoodacre.Cqrs.Examples.Tests`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests).

The tests use xUnit v3, Moq, and `DannyGoodacre.Testing`. Add the assertion helpers to your test project:

```shell
dotnet add package DannyGoodacre.Testing
```

xUnit v3 test projects are executable. Run the example tests with this command:

```shell
dotnet run --project tests/DannyGoodacre.Cqrs.Examples.Tests
```

## Result assertions

`DannyGoodacre.Testing` supplies assertion methods for results. Each method fails the test if the result has a different type.

| Method | Passes when | Returns |
| --- | --- | --- |
| `ShouldBeSuccess()` | The result is `Success` or `Success<T>`. | The value, for `IResult<T>` |
| `ShouldBeInvalid()` | The result is `Invalid` or `Invalid<T>`. | The `ValidationState` |
| `ShouldBeNotFound()` | The result is `NotFound` or `NotFound<T>`. | Nothing |
| `ShouldBeConflict()` / `ShouldBeConflict(message)` | The result is a `Conflict`, with this message if you give one. | The message, for the form with no parameter |
| `ShouldBeDomainError()` / `ShouldBeDomainError(message)` | The result is a `DomainError`, with this message if you give one. | The message, for the form with no parameter |
| `ShouldBeInternalError()` | The result is an `InternalError`. | The `Error` |
| `ShouldBeCanceled()` | The result is `Canceled`. | Nothing |

## Test approach

The example tests send each request through the real pipeline to a real SQLite database:

- `AddCustomerOnboarding` builds the same services as the application.
- Each test gets a new in-memory SQLite database.
- An EF Core interceptor records each SQL command and each transaction.
- Only the email sender is a mock.

With mocks of `IStateUnit` and `ITransactionUnit`, a test can only check the call sequence.
With a real database, a test can prove the commit, the rollback, and the absence of database calls.

## Test host

From [`Infrastructure/ExamplesTestHost.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/Infrastructure/ExamplesTestHost.cs):

```csharp
public static async Task<ExamplesTestHost> CreateAsync(Action<IServiceCollection>? configureServices = null)
{
    var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();

    var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
    var databaseActivity = new DatabaseActivity();

    var services = new ServiceCollection();

    services.AddLogging();
    services.AddCustomerOnboarding((_, options) => options
        .UseSqlite(connection)
        .AddInterceptors(databaseActivity));

    // The last registration wins, so the mock replaces the stub email sender.
    services.AddSingleton(emailSender.Object);

    configureServices?.Invoke(services);

    ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

    await using (AsyncServiceScope scope = provider.CreateAsyncScope())
    {
        await scope.ServiceProvider.GetRequiredService<ExamplesDbContext>().Database.EnsureCreatedAsync();
    }

    databaseActivity.Reset();

    return new ExamplesTestHost(connection, provider, emailSender, databaseActivity);
}
```

Know these facts about the host:

- The email sender mock is strict. A hook failure does not change the result of a command, so a test that expects no email verifies it with `Times.Never`.
- `configureServices` lets a test add services, for example an extra post-commit hook.
- `databaseActivity.Reset()` removes the commands from `EnsureCreatedAsync`. Each test then sees only the commands of its own requests.
- `CreateScope()` and `InScopeAsync()` create one scope for each request, as a web application does.

[`Infrastructure/DatabaseActivity.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/Infrastructure/DatabaseActivity.cs) implements `IDbCommandInterceptor` and `IDbTransactionInterceptor`.
It exposes `Commands`, `TransactionsStarted`, `TransactionsCommitted`, and `TransactionsRolledBack`.

## State command tests

From [`RegisterCustomerPipelineTests.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/RegisterCustomerPipelineTests.cs).

The success test reads the row back in a new scope. This proves that the pipeline called `SaveChangesAsync`:

```csharp
[Fact]
public async Task HandleAsync_WhenCommandIsValid_SavesCustomer()
{
    // Arrange
    await using var host = await ExamplesTestHost.CreateAsync();

    var command = new RegisterCustomerCommand("Jane Doe", "Jane@Example.com");

    // Act
    IResult<Guid> result = await SendAsync(host, command);

    // Assert
    Guid publicId = result.ShouldBeSuccess();

    Customer saved = await host.InScopeAsync(services => services
        .GetRequiredService<ExamplesDbContext>()
        .Customers
        .SingleAsync(c => c.PublicId == publicId, TestContext.Current.CancellationToken));

    // The database generated the primary key during the save.
    Assert.True(saved.Id > 0);
    Assert.Equal("Jane Doe", saved.Name);
    Assert.Equal("jane@example.com", saved.Email);
}
```

The validation test proves the short-circuit. No SQL command runs:

```csharp
[Fact]
public async Task HandleAsync_WhenValidationFails_ShortCircuitsBeforeTheDatabase()
{
    // Arrange
    await using var host = await ExamplesTestHost.CreateAsync();

    var command = new RegisterCustomerCommand("", "not-an-email");

    // Act
    IResult<Guid> result = await SendAsync(host, command);

    // Assert
    ValidationState validationState = result.ShouldBeInvalid();

    Assert.Equal(2, validationState.Errors.Count);
    Assert.Contains(nameof(RegisterCustomerCommand.Name), validationState.Errors.Keys);
    Assert.Contains(nameof(RegisterCustomerCommand.Email), validationState.Errors.Keys);

    // The handler did not run, and the pipeline did not call SaveChangesAsync.
    Assert.Empty(host.DatabaseActivity.Commands);
}
```

The file also tests a duplicate email. The second request returns `Conflict`, and the table still contains one row.

## Query tests

From [`GetCustomerPipelineTests.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/GetCustomerPipelineTests.cs):

```csharp
[Fact]
public async Task HandleAsync_WhenCustomerDoesNotExist_ReturnsNotFound()
{
    // Arrange
    await using var host = await ExamplesTestHost.CreateAsync();

    // Act
    IResult<CustomerDetails> result = await SendAsync(host, new GetCustomerQuery(Guid.NewGuid()));

    // Assert
    result.ShouldBeNotFound();
}
```

The file also tests a customer with a subscription, and an empty customer ID. The empty ID returns `Invalid`, and no SQL command runs.

## Transacted command tests

From [`OnboardCustomerPipelineTests.cs`](../../../tests/DannyGoodacre.Cqrs.Examples.Tests/OnboardCustomerPipelineTests.cs).

### Commit, one transaction, and hook timing

```csharp
[Fact]
public async Task HandleAsync_WhenAllStepsSucceed_CommitsOneTransactionAndThenSendsEmail()
{
    // Arrange
    await using var host = await ExamplesTestHost.CreateAsync();

    await using AsyncServiceScope scope = host.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<ExamplesDbContext>();

    var command = new OnboardCustomerCommand("Jane Doe", "jane@example.com", "PREMIUM");

    bool? transactionOpenWhenHookRan = null;

    host.EmailSender
        .Setup(x => x.SendWelcomeEmailAsync("jane@example.com", "Jane Doe", It.IsAny<CancellationToken>()))
        .Callback(() => transactionOpenWhenHookRan = dbContext.Database.CurrentTransaction is not null)
        .Returns(Task.CompletedTask);

    var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<OnboardCustomerCommand, Guid>>();

    // Act
    IResult<Guid> result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

    // Assert
    Guid publicId = result.ShouldBeSuccess();

    Assert.Equal((1, 1), await CountRowsAsync(host));

    // The subscription links to the primary key that the database generated in step 1.
    (int customerId, int subscriptionCustomerId) = await host.InScopeAsync(async services =>
    {
        var db = services.GetRequiredService<ExamplesDbContext>();

        Customer customer = await db.Customers.SingleAsync(c => c.PublicId == publicId, TestContext.Current.CancellationToken);
        Subscription subscription = await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken);

        return (customer.Id, subscription.CustomerId);
    });

    Assert.Equal(customerId, subscriptionCustomerId);

    // The child state command saved inside the outer transaction. It did not start a second one.
    Assert.Equal(1, host.DatabaseActivity.TransactionsStarted);
    Assert.Equal(1, host.DatabaseActivity.TransactionsCommitted);

    // The hook ran one time, after the commit.
    host.EmailSender.Verify(
        x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
        Times.Once);

    Assert.False(transactionOpenWhenHookRan);

    // ...
}
```

The test proves four facts:

- The customer and the subscription are both in the database.
- The subscription's `CustomerId` is the integer `Id` that the database generated when the child command saved the customer.
- The child `RegisterCustomerCommand` calls `SaveChangesAsync`, but only one transaction starts. See [COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md#why-istatecommand-is-safe-inside-itransactedstatecommand).
- When the hook runs, the `DbContext` has no open transaction. Thus, the commit is complete. See [POST-COMMIT-HOOKS.md](POST-COMMIT-HOOKS.md).

The hook resolves from the same scope as the handler, so the test reads `CurrentTransaction` from the same `DbContext`.

### Rollback

```csharp
// Step 1 wrote the customer to the database inside the transaction...
Assert.Contains(host.DatabaseActivity.Commands, sql => sql.Contains("INSERT INTO \"Customers\""));

// ...and the rollback removed it.
Assert.Equal(1, host.DatabaseActivity.TransactionsRolledBack);
Assert.Equal(0, host.DatabaseActivity.TransactionsCommitted);
Assert.Equal((0, 0), await CountRowsAsync(host));
```

The plan `GOLD` does not exist, so the child query returns `NotFound`.
The child command had already sent its `INSERT`. The rollback removed the row.

### Other transacted tests

| Test | Proof |
| --- | --- |
| `HandleAsync_WhenEmailIsInUse_ReturnsConflictFromTheChildCommand` | The child `Conflict` reaches the caller. No subscription is saved. |
| `HandleAsync_WhenValidationFails_ShortCircuitsBeforeTheTransaction` | The outer validator returns three errors. No transaction starts and no SQL command runs. |
| `HandleAsync_WhenChildCommandHasHook_RunsItOnlyAfterTheOuterCommit` | A hook on the child command runs after the outer commit, and does not run after a rollback. |

## Unit tests for a handler

A pipeline test checks the handler and the decorators together.
To test only the decision logic of an orchestrator handler, create the handler directly. Give it mocks of the child handlers.
The mocks return results, for example `Result<Guid>.Conflict("...")`. Then assert the result of the handler.

## Related documents

- [EXAMPLES.md](EXAMPLES.md)
- [COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md)
- [POST-COMMIT-HOOKS.md](POST-COMMIT-HOOKS.md)
- [RESULT-HANDLING.md](RESULT-HANDLING.md)
