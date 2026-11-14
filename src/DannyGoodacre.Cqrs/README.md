# DannyGoodacre.Cqrs

DannyGoodacre.Cqrs is a CQRS library for .NET 10. It sends each command and query through a fixed pipeline of decorators that handles logging, validation, transactions, and persistence, so your handlers contain only domain logic.

| Document | Subject |
| --- | --- |
| [EXAMPLES.md](docs/EXAMPLES.md) | The example project: a customer onboarding workflow on SQLite |
| [COMMANDS-AND-QUERIES.md](docs/COMMANDS-AND-QUERIES.md) | Request markers, handlers, validators, and child results |
| [POST-COMMIT-HOOKS.md](docs/POST-COMMIT-HOOKS.md) | Side effects that run after the commit, and their guarantees |
| [PERSISTENCE.md](docs/PERSISTENCE.md) | `IStateUnit`, `ITransactionUnit`, nested transactions, and retries |
| [TESTING.md](docs/TESTING.md) | Testing requests through the pipeline |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Pipeline order, exception handling, log events, scopes, and design principles |
| [DannyGoodacre.Primitives](../DannyGoodacre.Primitives/README.md) | `IResult`, `IResult<T>`, `Error`, and `ValidationState` |
| [DannyGoodacre.Cqrs.EntityFrameworkCore](../DannyGoodacre.Cqrs.EntityFrameworkCore/README.md) | The EF Core implementation of `IStateUnit` and `ITransactionUnit` |
| [DannyGoodacre.Testing](../DannyGoodacre.Testing/README.md) | xUnit assertions for results, `ValidationState`, and `Error` |
| [DannyGoodacre.Cqrs.Testing](../DannyGoodacre.Cqrs.Testing/README.md) | Test contexts for the pipeline decorators |

## Installation

```shell
dotnet add package DannyGoodacre.Cqrs
dotnet add package DannyGoodacre.Cqrs.EntityFrameworkCore
```

> **Note:** The packages are currently published to GitHub Packages only; publication to nuget.org is planned. Until then, add the feed with a GitHub token that has the `read:packages` scope:
>
> ```shell
> dotnet nuget add source https://nuget.pkg.github.com/dannygoodacre/index.json --name dannygoodacre --username <github-username> --password <token>
> ```

The package depends on [`DannyGoodacre.Primitives`](../DannyGoodacre.Primitives/README.md) for its result and validation types.

State commands (`IStateCommand` and `ITransactedStateCommand`) need an `IStateUnit` and an `ITransactionUnit` to save their changes. [`DannyGoodacre.Cqrs.EntityFrameworkCore`](../DannyGoodacre.Cqrs.EntityFrameworkCore/README.md) provides both for EF Core, the most widely used ORM for .NET. With any other data access, implement the two interfaces yourself; see [Custom implementation](docs/PERSISTENCE.md#custom-implementation). Queries and plain `ICommand`s, such as a command that only sends an email, need neither.

## Core concepts

| Concept | Purpose |
| --- | --- |
| `ICommand` | A command that does not save data to your database. |
| `IStateCommand` | A command that changes state. The pipeline calls `SaveChangesAsync` after the handler succeeds. |
| `ITransactedStateCommand` | A command that runs in one explicit transaction, so its child commands and queries succeed or roll back together. |
| `IQuery<T>` | A read-only request that returns a `T`. |
| Handler | Implements `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResult>`, or `IQueryHandler<TQuery, TResult>`. |
| `IValidator<T>` | Input rules that the pipeline runs before the handler. |
| `IPostCommitHook` | A side effect that runs only after the data is committed. |
| `IResult` / `IResult<T>` | The return value of every handler. Expected failures are results, not exceptions. |

All requests implement `IRequest`.

## Quick start

This code comes from the example project in [`src/DannyGoodacre.Cqrs.Examples`](../DannyGoodacre.Cqrs.Examples), which registers customers in a SQLite database. [EXAMPLES.md](docs/EXAMPLES.md) describes the whole project.

### 1. Define the command and its validator

From [`Customers/RegisterCustomerCommand.cs`](../DannyGoodacre.Cqrs.Examples/Customers/RegisterCustomerCommand.cs):

```csharp
// A state command. The pipeline calls SaveChangesAsync after the handler returns a success result.
public sealed record RegisterCustomerCommand(string Name, string Email) : IStateCommand;

public sealed class RegisterCustomerCommandValidator : IValidator<RegisterCustomerCommand>
{
    public void Validate(RegisterCustomerCommand request, ValidationState validationState)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            validationState.AddError(nameof(request.Name), "Name is required.");
        }

        // ... email rules ...
    }
}
```

The pipeline runs the validator before the handler. If it adds an error, the handler does not run.

### 2. Add the handler

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
            return new Conflict<Guid>("A customer with this email address already exists.");
        }

        var customer = new Customer
        {
            PublicId = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Email = email
        };

        customerRepository.Add(customer);

        return new Success<Guid>(customer.PublicId);
    }
}
```

The handler returns `Conflict` for a duplicate email instead of throwing, and it does not call `SaveChangesAsync`, because the pipeline does that once the handler returns a success.

### 3. Register the services

From [`ExamplesServiceCollectionExtensions.cs`](../DannyGoodacre.Cqrs.Examples/ExamplesServiceCollectionExtensions.cs):

```csharp
public IServiceCollection AddCustomerOnboarding(Action<IServiceProvider, DbContextOptionsBuilder> configureDatabase)
{
    services.AddDbContext<ExamplesDbContext>(configureDatabase);

    services.AddEntityFrameworkUnits<ExamplesDbContext>();

    // ... application services ...

    services.AddCqrsHandlers([typeof(ExamplesServiceCollectionExtensions).Assembly]);

    return services;
}
```

- `AddEntityFrameworkUnits<TDbContext>()` registers one scoped `DbContextTransaction<TDbContext>` as both `IStateUnit` and `ITransactionUnit`. State command handlers need both.
- `AddCqrsHandlers` scans the given assemblies, registers all handlers, validators, and post-commit hooks, and wraps each handler in the pipeline. You can call it more than once, for example once per module; each call wraps only the handlers it registers, and skips any handler, validator, or hook already registered.
- The pipeline needs logging. `WebApplication.CreateBuilder` registers it; in other hosts, call `services.AddLogging()`.

### 4. Send the command and read the result

From [`Program.cs`](../DannyGoodacre.Cqrs.Examples/Program.cs):

```csharp
await using AsyncServiceScope scope = provider.CreateAsyncScope();

var register = scope.ServiceProvider.GetRequiredService<ICommandHandler<RegisterCustomerCommand, Guid>>();

// A state command. The pipeline saves the customer after the handler succeeds.
IResult<Guid> registered = await register.HandleAsync(new RegisterCustomerCommand("Ann Lee", "ann@example.com"));

if (registered is Success<Guid>(var registeredId))
{
    Console.WriteLine($"Registered customer {registeredId}.");
}
```

Resolving the handler interface gives you the full pipeline, not the bare handler. In a web application, inject the interface into the endpoint; each request then gets its own scope. The pipeline never throws to the caller, so read the outcome from the result.

### 5. Run the example

From the repository root:

```shell
dotnet run --project src/DannyGoodacre.Cqrs.Examples        # console demonstration
dotnet run --project tests/DannyGoodacre.Cqrs.Examples.Tests # pipeline tests
```

## Features

- **One pipeline for all requests.** Logging, validation, transactions, and persistence apply to every handler without code in the handler.
- **Validation before data access.** A failed validator stops the request before any transaction or database call.
- **Automatic save.** The pipeline calls `SaveChangesAsync` after a successful `IStateCommand` handler.
- **Explicit transactions.** An `ITransactedStateCommand` runs its child commands and queries in one transaction that rolls back if any step fails.
- **Post-commit hooks.** Side effects such as emails run only after the data is committed.
- **No control-flow exceptions.** Handlers return results. The pipeline converts unexpected exceptions to `InternalError` and cancellations to `Canceled`.
- **Structured logs.** Each log entry has a fixed event ID from `CqrsLogEvent`.
- **Read-only queries.** `AddCqrsHandlers` throws at startup if a type implements both `IQuery<T>` and a state command marker.
- **One handler per request.** `AddCqrsHandlers` throws at startup if a request has more than one handler, or if a validator or hook would run twice because it derives from a concrete one. One exception lists every problem found.
