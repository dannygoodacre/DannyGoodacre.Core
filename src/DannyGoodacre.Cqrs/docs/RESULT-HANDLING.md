# Result handling

This document tells you how handlers and callers use results in the DannyGoodacre.Cqrs pipeline.

The result types are in the `DannyGoodacre.Primitives` package. That package documents the types, the factory methods, and the patterns:

- [Primitives README](../../DannyGoodacre.Primitives/README.md): the outcome types.
- [RESULTS.md](../../DannyGoodacre.Primitives/docs/RESULTS.md): factory methods, pattern matching, and `MapFailure`.
- [VALIDATION.md](../../DannyGoodacre.Primitives/docs/VALIDATION.md): `ValidationState`.
- [ERRORS.md](../../DannyGoodacre.Primitives/docs/ERRORS.md): `Error` and `InternalError`.

## Rules for handlers

1. Each handler must return `IResult` or `IResult<T>`.
2. Handlers must not throw exceptions for domain logic control flow.
3. Return a failure result for each expected failure. Examples are "not found", "out of stock", and "email in use".
4. Do not catch exceptions only to convert them. The pipeline converts them.

The reasons for these rules:

- The persistence decorators read `IsSuccess`. A failure result stops the save, causes a rollback, and stops the post-commit hook. No exception is necessary.
- The caller sees all outcomes in one `switch` expression.
- The logs show business failures as warnings, not as errors.

## Who creates each result

| Result | Created by | When |
| --- | --- | --- |
| `Success` | Handler | The operation completed. |
| `DomainError` | Handler | A business rule stops the operation. |
| `NotFound` | Handler | The data does not exist. |
| `Conflict` | Handler | The current data prevents the operation, for example a duplicate email. |
| `Invalid` | Validation decorator | The `IValidator<T>` adds one or more errors. |
| `Canceled` | Logging and persistence decorators | The cancellation token is canceled, or an `OperationCanceledException` occurs. |
| `InternalError` | Logging and persistence decorators | An unexpected exception occurs. |

[ARCHITECTURE.md](ARCHITECTURE.md#exception-and-cancellation-handling) lists each exception, its result, and its log event.

### Do not return Invalid from a handler

Write an `IValidator<T>` for the request. The validation decorator returns `Invalid` before the handler runs.
See [ARCHITECTURE.md](ARCHITECTURE.md#validation-short-circuit).

Validators are synchronous. Rules that need the database go in the handler. Return `Conflict` or `DomainError` for these rules:

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace Shop.Customers;

public sealed record RegisterCustomerCommand(string Name, string Email) : IStateCommand;

public interface IRegisteredCustomerRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    void Add(Customer customer);
}

public sealed class RegisterCustomerCommandHandler(IRegisteredCustomerRepository customerRepository)
    : ICommandHandler<RegisterCustomerCommand, Guid>
{
    public async Task<IResult<Guid>> HandleAsync(
        RegisterCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await customerRepository.EmailExistsAsync(command.Email, cancellationToken))
        {
            return Result<Guid>.Conflict("A customer with this email already exists.");
        }

        var customer = new Customer(Guid.CreateVersion7(), command.Name, command.Email);

        customerRepository.Add(customer);

        return Result<Guid>.Success(customer.Id);
    }
}
```

### Do not create InternalError or Canceled in a handler

Pass the `CancellationToken` to all asynchronous calls. Then a cancellation stops the work, and the pipeline returns `Canceled`.
If an unexpected exception occurs, let it go to the pipeline. The logging decorator logs it as event 1006 and returns `InternalError`.

## Send a child result to the caller

An orchestrator handler calls child handlers. Use these rules to return a child failure:

| Orchestrator returns | Child returns | Action |
| --- | --- | --- |
| `IResult` | `IResult` or `IResult<T>` | Return the child result. `IResult<T>` is also an `IResult`. |
| `IResult<T>` | `IResult<T>` with the same `T` | Return the child result. |
| `IResult<TOut>` | `IResult<TIn>` | Return `childResult.MapFailure<TOut>()`. |

[COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md#example-customer-onboarding) shows the first case.
This example shows the third case. The handler returns `IResult<Guid>`, but the child query returns `IResult<OrderSummary>`:

```csharp
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace Shop.Orders;

public sealed record ReorderCommand(Guid PreviousOrderId) : IStateCommand;

public sealed class ReorderCommandHandler(
    IQueryHandler<GetOrderByIdQuery, OrderSummary> getOrderHandler,
    IOrderRepository orderRepository)
    : ICommandHandler<ReorderCommand, Guid>
{
    public async Task<IResult<Guid>> HandleAsync(ReorderCommand command, CancellationToken cancellationToken = default)
    {
        IResult<OrderSummary> previousResult = await getOrderHandler.HandleAsync(
            new GetOrderByIdQuery(command.PreviousOrderId), cancellationToken);

        if (previousResult is not Success<OrderSummary>(var previous))
        {
            return previousResult.MapFailure<Guid>();
        }

        var order = new Order(
            Guid.CreateVersion7(),
            previous.CustomerId,
            previous.ProductSku,
            previous.Quantity,
            previous.Total);

        orderRepository.Add(order);

        return Result<Guid>.Success(order.Id);
    }
}
```

If the previous order does not exist, the caller receives `NotFound<Guid>`.

## Map results to HTTP responses

Put the mapping in one place at the edge of the application.
Match the non-generic failure types. They also match the generic types, so one method serves all value types.

```csharp
using DannyGoodacre.Primitives;
using HttpResult = Microsoft.AspNetCore.Http.IResult;
using IResult = DannyGoodacre.Primitives.IResult;

namespace Shop.Api;

public static class ResultHttpExtensions
{
    public static HttpResult ToHttpResult<T>(this IResult<T> result, Func<T, HttpResult> onSuccess)
        => result is Success<T>(var value)
            ? onSuccess(value)
            : result.ToFailureHttpResult();

    public static HttpResult ToHttpResult(this IResult result, Func<HttpResult> onSuccess)
        => result.IsSuccess
            ? onSuccess()
            : result.ToFailureHttpResult();

    private static HttpResult ToFailureHttpResult(this IResult result)
        => result switch
        {
            Invalid(var validationState) => Results.ValidationProblem(
                validationState.Errors.ToDictionary(e => e.Key, e => e.Value.ToArray())),
            NotFound => Results.NotFound(),
            Conflict(var message) => Results.Conflict(new { message }),
            DomainError(var message) => Results.UnprocessableEntity(new { message }),
            Canceled => Results.StatusCode(StatusCodes.Status499ClientClosedRequest),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
}
```

Use the extension methods in the endpoints:

```csharp
using DannyGoodacre.Cqrs;
using Shop.Api;
using Shop.Orders;

app.MapPost("/orders", async (
    PlaceOrderCommand command,
    ICommandHandler<PlaceOrderCommand, Guid> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(command, cancellationToken);

    return result.ToHttpResult(orderId => Results.Created($"/orders/{orderId}", new { orderId }));
});

app.MapPost("/orders/{orderId:guid}/cancel", async (
    Guid orderId,
    ICommandHandler<CancelOrderCommand> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new CancelOrderCommand(orderId), cancellationToken);

    return result.ToHttpResult(() => Results.NoContent());
});

app.MapGet("/orders/{orderId:guid}", async (
    Guid orderId,
    IQueryHandler<GetOrderByIdQuery, OrderSummary> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetOrderByIdQuery(orderId), cancellationToken);

    return result.ToHttpResult(summary => Results.Ok(summary));
});
```

> **Note:** ASP.NET Core and `DannyGoodacre.Primitives` both have a type named `IResult`. The `using` aliases in the example prevent the name conflict. In endpoints, use `var` for the handler result.

The `_` branch handles `InternalError`. Do not send exception details to the client. The pipeline already logged the exception.

## Do not throw for control flow

Incorrect:

```csharp
if (product.StockLevel < command.Quantity)
{
    throw new InvalidOperationException("Insufficient stock.");
}
```

The pipeline converts this exception to `InternalError`. The caller cannot see the cause. The log shows an error, not a business rule.

Correct:

```csharp
if (product.StockLevel < command.Quantity)
{
    return Result<Guid>.DomainError("Insufficient stock.");
}
```

## Test results

`DannyGoodacre.Testing` supplies assertion methods for results.

| Method | Passes when | Returns |
| --- | --- | --- |
| `ShouldBeSuccess()` | The result is `Success` or `Success<T>`. | The value, for `IResult<T>` |
| `ShouldBeInvalid()` | The result is `Invalid` or `Invalid<T>`. | The `ValidationState` |
| `ShouldBeNotFound()` | The result is `NotFound` or `NotFound<T>`. | Nothing |
| `ShouldBeDomainError(message)` | The result is a `DomainError` with this message. | Nothing |

[COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md#tests) has full pipeline tests that use these methods.

## Related documents

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md)
- [PERSISTENCE.md](PERSISTENCE.md)
