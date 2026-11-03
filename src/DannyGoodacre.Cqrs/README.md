# DannyGoodacre.Cqrs

DannyGoodacre.Cqrs is a CQRS library for .NET 10.
It sends each command and query through a fixed pipeline of decorators.
The pipeline adds logging, validation, transaction control, and state persistence.
Your handlers contain only domain logic.

## Contents

| Document | Subject |
| --- | --- |
| [README.md](README.md) | Installation, quick start, core concepts, and features |
| [EXAMPLES.md](docs/EXAMPLES.md) | The example project: a customer onboarding workflow on SQLite |
| [COMMANDS-AND-QUERIES.md](docs/COMMANDS-AND-QUERIES.md) | `ICommand`, `IStateCommand`, `ITransactedStateCommand`, `IQuery<T>`, and validators |
| [POST-COMMIT-HOOKS.md](docs/POST-COMMIT-HOOKS.md) | Side effects that run after the data is saved |
| [RESULT-HANDLING.md](docs/RESULT-HANDLING.md) | Results in the pipeline, child results, and HTTP mapping |
| [PERSISTENCE.md](docs/PERSISTENCE.md) | `IStateUnit`, `ITransactionUnit`, `DbContextTransaction<TDbContext>`, and EF Core retries |
| [TESTING.md](docs/TESTING.md) | Result assertions and xUnit pipeline tests on SQLite |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Pipeline flow, decorator order, logging, and design principles |
| [DannyGoodacre.Primitives](../DannyGoodacre.Primitives/README.md) | Reference for `IResult`, `IResult<T>`, `Error`, and `ValidationState` |

## Installation

Add the package to your application project:

```shell
dotnet add package DannyGoodacre.Cqrs
```

The package depends on `DannyGoodacre.Primitives`, which contains `IResult`, `IResult<T>`, and `ValidationState`.
See the [Primitives documentation](../DannyGoodacre.Primitives/README.md) for these types.

For Entity Framework Core persistence, add the integration package:

```shell
dotnet add package DannyGoodacre.Cqrs.EntityFrameworkCore
```

## Core concepts

| Concept | Purpose |
| --- | --- |
| Request | A command or a query. All requests implement `IRequest`. |
| `ICommand` | A command that does not save data to a database. |
| `IStateCommand` | A command that changes state. The pipeline calls `SaveChangesAsync` after the handler succeeds. |
| `ITransactedStateCommand` | A command that runs inside an explicit database transaction. Use it to run child commands and queries as one unit. |
| `IQuery<T>` | A read-only request that returns a value of type `T`. |
| Handler | A class that implements `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResult>`, or `IQueryHandler<TQuery, TResult>`. |
| `IValidator<T>` | Input rules for a request. The pipeline runs it before the handler. |
| `IPostCommitHook` | A side effect that runs only after the data is saved. |
| `IResult` / `IResult<T>` | The return value of every handler. Handlers do not throw exceptions for domain errors. |

## Quick start

The code in this section comes from the example project in [`src/DannyGoodacre.Cqrs.Examples`](../DannyGoodacre.Cqrs.Examples).
The project registers customers in an SQLite database. [EXAMPLES.md](docs/EXAMPLES.md) describes the full project.

### 1. Define the command

From [`Customers/RegisterCustomerCommand.cs`](../DannyGoodacre.Cqrs.Examples/Customers/RegisterCustomerCommand.cs):

```csharp
public sealed record RegisterCustomerCommand(string Name, string Email) : IStateCommand;
```

`IStateCommand` tells the pipeline to call `SaveChangesAsync` after the handler returns a success result.

### 2. Add a validator

The pipeline finds the validator and runs it before the handler. If the validator adds an error, the handler does not run.

```csharp
public sealed class RegisterCustomerCommandValidator : IValidator<RegisterCustomerCommand>
{
    public void Validate(RegisterCustomerCommand request, ValidationState validationState)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            validationState.AddError(nameof(request.Name), "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            validationState.AddError(nameof(request.Email), "Email is required.");
        }
        else if (!MailAddress.TryCreate(request.Email, out _))
        {
            validationState.AddError(nameof(request.Email), "Email format is invalid.");
        }
    }
}
```

### 3. Add the handler

The handler returns a `Conflict` result for a duplicate email. It does not throw an exception.
The handler does not call `SaveChangesAsync`. The pipeline does this.
The handler returns `PublicId`, which it creates. The database generates `Id` later, during the save. See [Entity keys](docs/COMMANDS-AND-QUERIES.md#entity-keys).

```csharp
public sealed class RegisterCustomerCommandHandler(ICustomerRepository customerRepository)
    : ICommandHandler<RegisterCustomerCommand, Guid>
{
    public async Task<IResult<Guid>> HandleAsync(
        RegisterCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        string email = command.Email.Trim().ToLowerInvariant();

        if (await customerRepository.EmailExistsAsync(email, cancellationToken))
        {
            return Result<Guid>.Conflict("A customer with this email address already exists.");
        }

        // The database generates Id when the pipeline saves, after the handler returns.
        // The handler creates PublicId, so it can return that key now.
        var customer = new Customer
        {
            PublicId = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Email = email
        };

        customerRepository.Add(customer);

        return Result<Guid>.Success(customer.PublicId);
    }
}
```

### 4. Register the services

From [`ExamplesServiceCollectionExtensions.cs`](../DannyGoodacre.Cqrs.Examples/ExamplesServiceCollectionExtensions.cs):

```csharp
public IServiceCollection AddCustomerOnboarding(Action<IServiceProvider, DbContextOptionsBuilder> configureDatabase)
{
    services.AddDbContext<ExamplesDbContext>(configureDatabase);

    services.AddEntityFrameworkUnits<ExamplesDbContext>();

    services.AddScoped<ICustomerRepository, CustomerRepository>();
    services.AddScoped<IPlanRepository, PlanRepository>();
    services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
    services.AddScoped<IEmailSender, LoggingEmailSender>();
    services.TryAddSingleton(TimeProvider.System);

    services.AddCqrsHandlers([typeof(ExamplesServiceCollectionExtensions).Assembly]);

    return services;
}
```

`AddEntityFrameworkUnits` registers one scoped `DbContextTransaction<ExamplesDbContext>` for both `IStateUnit` and `ITransactionUnit`.
`AddCqrsHandlers` scans the assemblies. It registers all handlers, validators, and post-commit hooks.
Then it wraps each handler in the pipeline decorators.

Obey these rules:

- Call `AddCqrsHandlers` one time. Give it all the assemblies that contain handlers.
- Register `IStateUnit` and `ITransactionUnit` before you resolve a command handler. The pipeline needs them for all command handlers. `AddEntityFrameworkUnits<TDbContext>()` registers both.
- Register logging. `WebApplication.CreateBuilder` does this for you. In other hosts, call `services.AddLogging()`.

### 5. Send the command and read the result

From [`Program.cs`](../DannyGoodacre.Cqrs.Examples/Program.cs):

```csharp
await using AsyncServiceScope scope = provider.CreateAsyncScope();

var register = scope.ServiceProvider.GetRequiredService<ICommandHandler<RegisterCustomerCommand, Guid>>();

// A state command. The pipeline saves the customer after the handler succeeds.
IResult<Guid> registered = await register.HandleAsync(new RegisterCustomerCommand("Ann Lee", "ann@example.com"));

if (registered is Success<Guid>(var annId))
{
    Console.WriteLine($"Registered ann@example.com as customer {annId}.");
}
```

The example uses one scope for the whole demonstration. A web application creates one scope for each request.
Resolve the handler interface from the scope. The container gives you the full pipeline, not the bare handler.
In a web application, inject the handler interface into the endpoint instead.
Read the outcome with a `switch` expression. The pipeline does not throw exceptions to the caller.
[RESULT-HANDLING.md](docs/RESULT-HANDLING.md#map-results-to-http-responses) shows how to map results to HTTP responses.

### 6. Run the example

Run these commands from the repository root:

```shell
dotnet run --project src/DannyGoodacre.Cqrs.Examples
dotnet run --project tests/DannyGoodacre.Cqrs.Examples.Tests
```

The first command runs the console demonstration. The second command runs the pipeline tests.

## Features

- **One pipeline for all requests.** Logging, validation, transactions, and persistence apply to each handler. You do not write this code in each handler.
- **Validation before data access.** A failed validator stops the pipeline. The handler and the database do not run.
- **Automatic save.** The pipeline calls `SaveChangesAsync` after a successful `IStateCommand` handler.
- **Explicit transactions.** An `ITransactedStateCommand` runs its child commands and queries in one transaction. If one step fails, all changes roll back.
- **Post-commit hooks.** Side effects, such as emails, run only after the data is saved.
- **No control-flow exceptions.** Handlers return `IResult` values. The pipeline converts unexpected exceptions to `InternalError` and cancellations to `Canceled`.
- **Structured logs.** Each log entry has a fixed event ID from `CqrsLogEvent`.
- **Read-only queries.** At startup, `AddCqrsHandlers` throws `InvalidOperationException` if a type implements both `IQuery<T>` and a state command marker.

## Next steps

1. Read [EXAMPLES.md](docs/EXAMPLES.md) for a tour of the example project.
2. Read [COMMANDS-AND-QUERIES.md](docs/COMMANDS-AND-QUERIES.md) to select the correct marker interface.
3. Read [POST-COMMIT-HOOKS.md](docs/POST-COMMIT-HOOKS.md) to run side effects after a save.
4. Read [RESULT-HANDLING.md](docs/RESULT-HANDLING.md) to return and read results in the pipeline.
5. Read [PERSISTENCE.md](docs/PERSISTENCE.md) to connect the pipeline to EF Core.
6. Read [TESTING.md](docs/TESTING.md) to test your handlers.
7. Read [ARCHITECTURE.md](docs/ARCHITECTURE.md) to learn the pipeline order.
8. Read the [Primitives documentation](../DannyGoodacre.Primitives/README.md) for the full result type reference.
