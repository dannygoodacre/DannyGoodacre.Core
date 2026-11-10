# DannyGoodacre.Testing

xUnit assertion helpers for the [DannyGoodacre.Primitives](../DannyGoodacre.Primitives/README.md) types: results, `ValidationState`, and `Error`. Each assertion checks the type and returns the contents, so a test needs no casts.

```csharp
Guid customerId = result.ShouldBeSuccess();

result.ShouldBeConflict("A customer with this email address already exists.");

ValidationState validationState = result.ShouldBeInvalid();
validationState.ShouldHaveError("Email", "Email is required.");
```

## Installation

```shell
dotnet add package DannyGoodacre.Testing
```

> **Note:** The package is currently published to GitHub Packages only; publication to nuget.org is planned. See the [installation note](../DannyGoodacre.Cqrs/README.md#installation) for adding the feed.

The assertions use xUnit v3 (`xunit.v3.assert`). All methods are in the `DannyGoodacre.Testing` namespace.

## Result assertions

These extend `IResult` and `IResult<T>`. Each fails the test if the result is a different type, and matches both the generic and non-generic forms, so `ShouldBeNotFound()` passes for `NotFound` and `NotFound<T>`.

| Method | Passes when | Returns |
| --- | --- | --- |
| `ShouldBeSuccess()` / `ShouldBeSuccess(expected)` | The result is a success, with this value if given. | The value, for `IResult<T>` with no argument |
| `ShouldBeInvalid()` / `ShouldBeInvalid(expected)` | The result is `Invalid`, with matching errors if a state is given. | The `ValidationState`, with no argument |
| `ShouldBeNotFound()` | The result is `NotFound`. | Nothing |
| `ShouldBeConflict()` / `ShouldBeConflict(message)` | The result is `Conflict`, with this message if given. | The message, with no argument |
| `ShouldBeDomainError()` / `ShouldBeDomainError(message)` | The result is `DomainError`, with this message if given. | The message, with no argument |
| `ShouldBeInternalError()` / `ShouldBeInternalError(error)` / `ShouldBeInternalError(exception)` | The result is `InternalError`, equal to this `Error` or carrying this exception if given. | The `Error`, with no argument |
| `ShouldBeCanceled()` | The result is `Canceled`. | Nothing |

On an `IResult`, `ShouldBeSuccess()` only checks `IsSuccess`. On an `IResult<T>`, it requires a `Success<T>` and returns its value.

## ValidationState assertions

| Method | Passes when |
| --- | --- |
| `ShouldHaveNoErrors()` | The state has no errors. |
| `ShouldHaveError(property, error)` | The property has this error, among any others. |
| `ShouldHaveErrorsFor(property, params errors)` | The property has exactly these errors, in this order. |
| `ShouldMatch(expected)` | Both states have the same properties and the same errors for each. Property names are compared case-insensitively. |

## Error assertions

| Method | Passes when | Returns |
| --- | --- | --- |
| `ShouldHaveMessage(message)` | `Message` equals the value. | Nothing |
| `ShouldHaveCode(code)` | `Code` equals the value. | Nothing |
| `ShouldHaveException<TException>()` | `Exception` is a `TException` or a subtype. | The exception |
| `ShouldMatch(message, code = "General")` / `ShouldMatch(expected)` | The message and code match, and for an `Error` argument, the same exception instance. | Nothing |

```csharp
Error error = result.ShouldBeInternalError();

error.ShouldHaveCode("PAYMENT_GATEWAY_UNAVAILABLE");
error.ShouldHaveException<HttpRequestException>();
```
