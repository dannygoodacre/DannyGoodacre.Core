# Result handling

This document tells you how to return and read results in DannyGoodacre.Cqrs.
The result types are in the `DannyGoodacre.Primitives` namespace.

## Rules

1. Each handler must return `IResult` or `IResult<T>`.
2. Handlers must not throw exceptions for domain logic control flow.
3. Return a failure result for each expected failure. Examples are "not found", "out of stock", and "email in use".
4. Use exceptions only for unexpected faults. The pipeline converts them to `InternalError`.

The reasons for these rules:

- The pipeline reads `IsSuccess` to decide to save, commit, or run hooks. A failure result causes a rollback with no exception.
- The caller sees all possible outcomes in one `switch` expression.
- Exceptions are slow and hide the control flow.

## Result types

| Type | Meaning | Created by |
| --- | --- | --- |
| `Success` / `Success<T>` | The operation completed. `Success<T>` contains the value. | Handler |
| `DomainError` / `DomainError<T>` | A business rule stopped the operation. | Handler |
| `NotFound` / `NotFound<T>` | The requested data does not exist. | Handler |
| `Conflict` / `Conflict<T>` | The operation conflicts with the current state, for example a duplicate key. | Handler |
| `Invalid` / `Invalid<T>` | The input failed validation. It contains the `ValidationState`. | Validation decorator |
| `InternalError` / `InternalError<T>` | An unexpected exception occurred. It contains the exception. | Logging and persistence decorators |
| `Canceled` / `Canceled<T>` | The cancellation token was canceled. | Logging and persistence decorators |

`IResult<T>` is also an `IResult`. All result types have the `IsSuccess` and `IsFailure` properties.

## Return results from a handler

### Success

```csharp
// No value.
return Result.Success();

// With a value.
return Result<Guid>.Success(order.Id);
```

### NotFound

```csharp
Order? order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);

if (order is null)
{
    return Result.NotFound();
}
```

In a query handler, use the generic form:

```csharp
return summary is null
    ? Result<OrderSummary>.NotFound()
    : Result<OrderSummary>.Success(summary);
```

### DomainError

```csharp
if (product.StockLevel < command.Quantity)
{
    return Result<Guid>.DomainError("Insufficient stock.");
}
```

Write the message for the end user. Do not put internal details in the message.

### Conflict

Use `Conflict` when the request is valid, but the current data prevents it.

```csharp
public sealed record RegisterCustomerCommand(string Name, string Email) : IStateCommand;

public interface ICustomerRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    void Add(Customer customer);
}

public sealed class RegisterCustomerCommandHandler(ICustomerRepository customerRepository)
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

### Invalid

Do not return `Invalid` from a handler. Write an `IValidator<T>` for the request.
The validation decorator returns `Invalid` before the handler runs. See [ARCHITECTURE.md](ARCHITECTURE.md#validation-short-circuit).

```csharp
public sealed class RegisterCustomerCommandValidator : IValidator<RegisterCustomerCommand>
{
    public void Validate(RegisterCustomerCommand request, ValidationState validationState)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            validationState.AddError(nameof(request.Name), "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            validationState.AddError(nameof(request.Email), "A valid email address is required.");
        }
    }
}
```

The validator records all errors. It does not stop at the first error.
`ValidationState.Errors` contains the errors. The key is the property name.

### InternalError and Canceled

Do not create `InternalError` or `Canceled` in a handler. Do not catch exceptions only to convert them.
The pipeline does this for you:

| Exception from the handler | Result |
| --- | --- |
| `OperationCanceledException` | `Canceled` |
| Other exception | `InternalError`. The logging decorator logs the exception as event 1006. |

Pass the `CancellationToken` to all asynchronous calls. Then cancellation stops the work and produces `Canceled`.

## Read results

### Check for success

Use `IsSuccess` or `IsFailure` when you do not need the value.

```csharp
IResult scheduleResult = await scheduleEmailHandler.HandleAsync(scheduleCommand, cancellationToken);

if (scheduleResult.IsFailure)
{
    return scheduleResult;
}
```

### Get the value

Use a positional pattern on `Success<T>` to get the value.

```csharp
IResult<Guid> createResult = await createCustomerHandler.HandleAsync(createCommand, cancellationToken);

if (createResult is not Success<Guid>(var customerId))
{
    return createResult;
}

// customerId is available here.
```

### Send a child failure to the caller

An orchestrator handler can return a child failure result without change.
The caller then sees the original failure type, for example `NotFound`.

```csharp
IResult<string> templateResult = await getTemplateHandler.HandleAsync(templateQuery, cancellationToken);

if (templateResult is not Success<string>(var template))
{
    return templateResult;
}
```

The orchestrator in this example returns `IResult`. `IResult<string>` is also an `IResult`, so the code compiles.

### Map results to HTTP responses

Use a `switch` expression at the edge of the application. Put the mapping in one extension method.

```csharp
using DannyGoodacre.Primitives;

namespace Shop.Api;

public static class ResultHttpExtensions
{
    public static Microsoft.AspNetCore.Http.IResult ToHttpResult<T>(
        this IResult<T> result,
        Func<T, Microsoft.AspNetCore.Http.IResult> onSuccess)
        where T : notnull
        => result switch
        {
            Success<T>(var value) => onSuccess(value),
            Invalid<T> => Results.BadRequest(),
            NotFound<T> => Results.NotFound(),
            Conflict<T> => Results.Conflict(),
            DomainError<T> => Results.UnprocessableEntity(),
            Canceled<T> => Results.StatusCode(StatusCodes.Status499ClientClosedRequest),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
}
```

Use the extension method in an endpoint:

```csharp
app.MapGet("/orders/{orderId:guid}", async (
    Guid orderId,
    IQueryHandler<GetOrderByIdQuery, OrderSummary> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetOrderByIdQuery(orderId), cancellationToken);

    return result.ToHttpResult(summary => Results.Ok(summary));
});
```

> **Note:** ASP.NET Core and `DannyGoodacre.Primitives` both have a type named `IResult`. Use the full name for one of them, as in the example above.

The `_` branch handles `InternalError` and all unknown types. Do not send exception details to the client.
The pipeline already logged the exception.

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

```csharp
Guid orderId = result.ShouldBeSuccess();

ValidationState validationState = result.ShouldBeInvalid();
Assert.Contains(nameof(PlaceOrderCommand.Quantity), validationState.Errors.Keys);

result.ShouldBeNotFound();

result.ShouldBeDomainError("Insufficient stock.");
```

[COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md#tests) has full pipeline tests.

## Related documents

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md)
- [PERSISTENCE.md](PERSISTENCE.md)
