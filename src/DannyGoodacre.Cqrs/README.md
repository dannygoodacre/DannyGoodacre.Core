# DannyGoodacre.Cqrs

DannyGoodacre.Cqrs is a CQRS library for .NET 10.
It sends each command and query through a fixed pipeline of decorators.
The pipeline adds logging, validation, transaction control, and state persistence.
Your handlers contain only domain logic.

## Contents

| Document | Subject |
| --- | --- |
| [README.md](README.md) | Installation, quick start, core concepts, and features |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Pipeline flow, decorator order, and design principles |
| [COMMANDS-AND-QUERIES.md](docs/COMMANDS-AND-QUERIES.md) | `ICommand`, `IStateCommand`, `ITransactedStateCommand`, `IQuery<T>`, and `IPostCommitHook` |
| [PERSISTENCE.md](docs/PERSISTENCE.md) | `IStateUnit`, `ITransactionUnit`, `DbContextTransactionUnit`, and EF Core retries |
| [RESULT-HANDLING.md](docs/RESULT-HANDLING.md) | Results in the pipeline, child results, and HTTP mapping |
| [DannyGoodacre.Primitives](../DannyGoodacre.Primitives/README.md) | Reference for `IResult`, `IResult<T>`, `Error`, and `ValidationState` |

## Installation

Add the package to your application project:

```shell
dotnet add package DannyGoodacre.Cqrs
```

The package depends on `DannyGoodacre.Primitives`. `DannyGoodacre.Primitives` contains `IResult`, `IResult<T>`, and `ValidationState`.
See the [Primitives documentation](../DannyGoodacre.Primitives/README.md) for these types.

For tests, add the assertion helpers:

```shell
dotnet add package DannyGoodacre.Testing
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

This example places an order in an order processing system.

### 1. Define the command

```csharp
using DannyGoodacre.Cqrs;

namespace Shop.Orders;

public sealed record PlaceOrderCommand(Guid CustomerId, string ProductSku, int Quantity) : IStateCommand;
```

### 2. Add a validator

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace Shop.Orders;

public sealed class PlaceOrderCommandValidator : IValidator<PlaceOrderCommand>
{
    public void Validate(PlaceOrderCommand request, ValidationState validationState)
    {
        if (request.CustomerId == Guid.Empty)
        {
            validationState.AddError(nameof(request.CustomerId), "Customer ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ProductSku))
        {
            validationState.AddError(nameof(request.ProductSku), "Product SKU is required.");
        }

        if (request.Quantity <= 0)
        {
            validationState.AddError(nameof(request.Quantity), "Quantity must be greater than zero.");
        }
    }
}
```

### 3. Add the handler

The handler does not call `SaveChangesAsync`. The pipeline does this for `IStateCommand` requests.

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace Shop.Orders;

public sealed class PlaceOrderCommandHandler(
    IProductRepository productRepository,
    IOrderRepository orderRepository)
    : ICommandHandler<PlaceOrderCommand, Guid>
{
    public async Task<IResult<Guid>> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        Product? product = await productRepository.GetBySkuAsync(command.ProductSku, cancellationToken);

        if (product is null)
        {
            return Result<Guid>.NotFound();
        }

        if (product.StockLevel < command.Quantity)
        {
            return Result<Guid>.DomainError("Insufficient stock.");
        }

        product.StockLevel -= command.Quantity;

        var order = new Order(
            Guid.CreateVersion7(),
            command.CustomerId,
            product.Sku,
            command.Quantity,
            product.UnitPrice * command.Quantity);

        orderRepository.Add(order);

        return Result<Guid>.Success(order.Id);
    }
}
```

### 4. Add a post-commit hook

The hook sends the confirmation email only after the order is saved.

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace Shop.Orders;

public sealed class SendOrderConfirmationHook(IEmailSender emailSender)
    : IPostCommitHook<PlaceOrderCommand, Guid>
{
    public Task AfterCommitAsync(
        PlaceOrderCommand command,
        IResult<Guid> result,
        CancellationToken cancellationToken = default)
        => result is Success<Guid>(var orderId)
            ? emailSender.SendOrderConfirmationAsync(command.CustomerId, orderId, cancellationToken)
            : Task.CompletedTask;
}
```

### 5. Register the services

`AddCqrsHandlers` scans the assemblies. It registers all handlers, validators, and post-commit hooks.
Then it wraps each handler in the pipeline decorators.

```csharp
using DannyGoodacre.Cqrs;
using Microsoft.EntityFrameworkCore;
using Shop.Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Shop"),
        sql => sql.EnableRetryOnFailure()));

// Persistence units. One scoped instance serves both interfaces.
builder.Services.AddScoped<ITransactionUnit>(sp => new DbContextTransactionUnit(sp.GetRequiredService<ShopDbContext>()));
builder.Services.AddScoped<IStateUnit>(sp => sp.GetRequiredService<ITransactionUnit>());

// Application services.
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Handlers, validators, hooks, and decorators.
builder.Services.AddCqrsHandlers(typeof(PlaceOrderCommand).Assembly);

var app = builder.Build();
```

Obey these rules:

- Call `AddCqrsHandlers` one time. Give it all the assemblies that contain handlers.
- Register `IStateUnit` and `ITransactionUnit` before you resolve a command handler. The pipeline needs them for all command handlers.
- Register logging. `WebApplication.CreateBuilder` does this for you. In other hosts, call `services.AddLogging()`.

### 6. Send the command and read the result

Inject the handler interface. The container gives you the full pipeline, not the bare handler.

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Shop.Orders;

app.MapPost("/orders", async (
    PlaceOrderCommand command,
    ICommandHandler<PlaceOrderCommand, Guid> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(command, cancellationToken);

    return result switch
    {
        Success<Guid>(var orderId) => Results.Created($"/orders/{orderId}", new { orderId }),
        Invalid(var validationState) => Results.ValidationProblem(
            validationState.Errors.ToDictionary(e => e.Key, e => e.Value.ToArray())),
        NotFound => Results.NotFound(),
        DomainError(var message) => Results.UnprocessableEntity(new { message }),
        _ => Results.Problem()
    };
});
```

Use `var` for the result. ASP.NET Core also has a type named `IResult`.
[RESULT-HANDLING.md](docs/RESULT-HANDLING.md#map-results-to-http-responses) shows a reusable mapping for all result types.

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

1. Read [ARCHITECTURE.md](docs/ARCHITECTURE.md) to learn the pipeline order.
2. Read [COMMANDS-AND-QUERIES.md](docs/COMMANDS-AND-QUERIES.md) to select the correct marker interface.
3. Read [PERSISTENCE.md](docs/PERSISTENCE.md) to connect the pipeline to EF Core.
4. Read [RESULT-HANDLING.md](docs/RESULT-HANDLING.md) to return and read results in the pipeline.
5. Read the [Primitives documentation](../DannyGoodacre.Primitives/README.md) for the full result type reference.
