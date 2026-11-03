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
| `Canceled` | Logging and persistence decorators | The caller's cancellation token is canceled. |
| `InternalError` | Logging and persistence decorators | An unexpected exception occurs, including an `OperationCanceledException` when the caller did not cancel. |

[ARCHITECTURE.md](ARCHITECTURE.md#exception-and-cancellation-handling) lists each exception, its result, and its log event.

### Do not return Invalid from a handler

Write an `IValidator<T>` for the request. The validation decorator returns `Invalid` before the handler runs.
See [ARCHITECTURE.md](ARCHITECTURE.md#validation-short-circuit).

Validators are synchronous. Rules that need the database go in the handler. Return `Conflict` or `DomainError` for these rules.
`RegisterCustomerCommandHandler` checks for a duplicate email in the database.
From [`Customers/RegisterCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Customers/RegisterCustomerCommand.cs):

```csharp
if (await customerRepository.EmailExistsAsync(email, cancellationToken))
{
    return Result<Guid>.Conflict("A customer with this email address already exists.");
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

`OnboardCustomerCommandHandler` returns `IResult<Guid>`. It uses the second and third cases.
From [`Onboarding/OnboardCustomerCommand.cs`](../../DannyGoodacre.Cqrs.Examples/Onboarding/OnboardCustomerCommand.cs):

```csharp
IResult<Guid> registerResult = await registerCustomerHandler.HandleAsync(
    new RegisterCustomerCommand(command.Name, command.Email), cancellationToken);

if (registerResult is not Success<Guid>(var customerPublicId))
{
    return registerResult;
}

IResult<PlanDetails> planResult = await getPlanHandler.HandleAsync(
    new GetPlanQuery(command.PlanCode), cancellationToken);

if (planResult is not Success<PlanDetails>(var plan))
{
    return planResult.MapFailure<Guid>();
}
```

- If the email is in use, the caller receives the `Conflict<Guid>` from the child command.
- If the plan does not exist, the caller receives `NotFound<Guid>`. `MapFailure` converts the `NotFound<PlanDetails>` from the query.

The transaction decorator sees the failure and rolls back. The handler does not need to know about the transaction.

## Read results in the caller

The caller reads the result with a `switch` expression. It does not catch exceptions.
From [`Program.cs`](../../DannyGoodacre.Cqrs.Examples/Program.cs):

```csharp
IResult<Guid> result = await onboard.HandleAsync(command);

Console.WriteLine(result switch
{
    Success<Guid>(var customerId) => $"Onboarded {command.Email} as customer {customerId}.",
    Invalid(var validationState) => $"The request is not valid:{Environment.NewLine}{validationState}",
    Conflict(var message) => $"Conflict: {message}",
    NotFound => $"Plan '{command.PlanCode}' does not exist. No data was saved.",
    _ => $"The request failed: {result}"
});
```

Put `Success<T>` first. Use the non-generic failure types, for example `Conflict`. They also match the generic types, for example `Conflict<Guid>`.
Keep a `_` branch for `InternalError`, `Canceled`, and other results.

## Map results to HTTP responses

A web application maps results to HTTP responses. Put the mapping in one place at the edge of the application.
Match the non-generic failure types. They also match the generic types, so one method serves all value types.

The example project is a console application, so it does not contain this code. The code uses ASP.NET Core minimal APIs.

```csharp
using DannyGoodacre.Primitives;
using HttpResult = Microsoft.AspNetCore.Http.IResult;
using IResult = DannyGoodacre.Primitives.IResult;

namespace CustomerApi;

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

Use the extension methods in the endpoints. These endpoints send the requests of the example project:

```csharp
using CustomerApi;
using DannyGoodacre.Cqrs;
using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Onboarding;

app.MapPost("/customers", async (
    OnboardCustomerCommand command,
    ICommandHandler<OnboardCustomerCommand, Guid> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(command, cancellationToken);

    return result.ToHttpResult(customerId => Results.Created($"/customers/{customerId}", new { customerId }));
});

app.MapGet("/customers/{customerId:guid}", async (
    Guid customerId,
    IQueryHandler<GetCustomerQuery, CustomerDetails> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetCustomerQuery(customerId), cancellationToken);

    return result.ToHttpResult(customer => Results.Ok(customer));
});
```

A request with an unknown plan code then returns 404. A request with a duplicate email returns 409.

> **Note:** ASP.NET Core and `DannyGoodacre.Primitives` both have a type named `IResult`. The `using` aliases in the example prevent the name conflict. In endpoints, use `var` for the handler result.

The `_` branch handles `InternalError`. Do not send exception details to the client. The pipeline already logged the exception.

## Do not throw for control flow

Incorrect:

```csharp
if (await customerRepository.EmailExistsAsync(email, cancellationToken))
{
    throw new InvalidOperationException("A customer with this email address already exists.");
}
```

The pipeline converts this exception to `InternalError`. The caller cannot see the cause. The log shows an error, not a business rule.

Correct:

```csharp
if (await customerRepository.EmailExistsAsync(email, cancellationToken))
{
    return Result<Guid>.Conflict("A customer with this email address already exists.");
}
```

## Test results

[TESTING.md](TESTING.md#result-assertions) lists the result assertion methods and has full pipeline tests.

## Related documents

- [COMMANDS-AND-QUERIES.md](COMMANDS-AND-QUERIES.md)
- [EXAMPLES.md](EXAMPLES.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [TESTING.md](TESTING.md)
