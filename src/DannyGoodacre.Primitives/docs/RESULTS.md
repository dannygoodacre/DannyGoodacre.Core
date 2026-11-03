# Results

This document describes the result types in DannyGoodacre.Primitives.
It tells you how to create results, read results, and convert failures between value types.

## Interfaces

| Interface | Purpose |
| --- | --- |
| `IResult` | The base contract for all results. It has the `IsSuccess` and `IsFailure` properties. |
| `IResult<out T>` | A result that contains a value of type `T` when it is a success. It derives from `IResult`. |
| `ISuccessResult` | A marker for success results. `Success` and `Success<T>` implement it. |

`IResult<out T>` is covariant. Thus, an `IResult<string>` is also an `IResult<object>`.

### IsSuccess and IsFailure

`IsSuccess` is `true` when the result implements `ISuccessResult`. `IsFailure` is the opposite of `IsSuccess`.

The two properties are default interface members of `IResult`. C# shows them through a variable of an interface type.
The factory methods return `IResult` or `IResult<T>`, so the properties are available, also with `var`:

```csharp
var result = Result.DomainError("The basket is empty.");

bool failed = result.IsFailure;
```

If you create a result with a constructor, for example `new DomainError("...")`, the variable has the concrete type.
Then cast it to `IResult` to read the properties.

## Outcome types

All outcome types are records. Each generic type derives from its non-generic type.

| Non-generic type | Generic type | Properties | Meaning |
| --- | --- | --- | --- |
| `Success` | `Success<T>` | `Value` (generic only) | The operation completed. |
| `DomainError` | `DomainError<T>` | `Message` | A business rule stopped the operation. |
| `NotFound` | `NotFound<T>` | None | The requested data does not exist. |
| `Conflict` | `Conflict<T>` | `Message` | The current state prevents the operation, for example a duplicate key. |
| `Invalid` | `Invalid<T>` | `ValidationState` | The input is not valid. See [VALIDATION.md](VALIDATION.md). |
| `InternalError` | `InternalError<T>` | `Error` | An unexpected fault occurred. See [ERRORS.md](ERRORS.md). |
| `Canceled` | `Canceled<T>` | None | The operation was canceled. |

`Success<T>` does not derive from `Success`. Both types implement `ISuccessResult`.

### Select an outcome type

| Condition | Type |
| --- | --- |
| The input breaks a format or range rule. | `Invalid` |
| The data that the input identifies does not exist. | `NotFound` |
| The data exists, but a stored value blocks the operation. Examples are a duplicate email or a stale version. | `Conflict` |
| The data exists, but a business rule blocks the operation. An example is a refund after the refund period. | `DomainError` |
| The caller canceled the operation. | `Canceled` |
| An exception or other unexpected fault occurred. | `InternalError` |

## Create results

Use the factory methods on the static `Result` and `Result<T>` classes.

Each `Result` method returns `IResult`, and each `Result<T>` method returns `IResult<T>`. The table shows the type of the object that each method creates.

| `Result` method | Creates | `Result<T>` method | Creates |
| --- | --- | --- | --- |
| `Success()` | `Success` | `Success(T value)` | `Success<T>` |
| `Success<T>(T value)` | `Success<T>` (returns `IResult<T>`) | | |
| `DomainError(string message)` | `DomainError` | `DomainError(string message)` | `DomainError<T>` |
| `NotFound()` | `NotFound` | `NotFound()` | `NotFound<T>` |
| `Conflict(string message)` | `Conflict` | `Conflict(string message)` | `Conflict<T>` |
| `Invalid(ValidationState state)` | `Invalid` | `Invalid(ValidationState state)` | `Invalid<T>` |
| `InternalError(Error error)` | `InternalError` | `InternalError(Error error)` | `InternalError<T>` |
| `InternalError(string message)` | `InternalError` | `InternalError(string message)` | `InternalError<T>` |
| `InternalError(Exception exception)` | `InternalError` | `InternalError(Exception exception)` | `InternalError<T>` |
| `Canceled()` | `Canceled` | `Canceled()` | `Canceled<T>` |

`Result.Success(value)` infers `T` from the value. In a method that returns `IResult<T>`, all other failure results need `Result<T>`, because a non-generic failure is not an `IResult<T>`.

```csharp
using DannyGoodacre.Primitives;

namespace Shop.Customers;

public sealed record Customer(Guid Id, string Email, bool IsActive);

public interface ICustomerStore
{
    Customer? FindByEmail(string email);
}

public sealed class CustomerLookup(ICustomerStore store)
{
    public IResult<Customer> FindActive(string email)
    {
        Customer? customer = store.FindByEmail(email);

        if (customer is null)
        {
            return Result<Customer>.NotFound();
        }

        if (!customer.IsActive)
        {
            return Result<Customer>.DomainError("The customer account is closed.");
        }

        return Result.Success(customer);
    }
}
```

You can also use the record constructors, for example `new NotFound<Customer>()`. The factory methods are shorter.

## Read results

### Check the outcome

Use `IsSuccess` or `IsFailure` when you do not need the value or the failure details.

```csharp
IResult<Customer> result = lookup.FindActive("jane@example.com");

if (result.IsFailure)
{
    return;
}
```

### Get the value

Use a positional pattern on `Success<T>`.

```csharp
if (result is Success<Customer>(var customer))
{
    Console.WriteLine(customer.Email);
}
```

### Match all outcomes

Use a `switch` expression. The records with properties support positional patterns.

```csharp
string message = result switch
{
    Success<Customer>(var customer) => $"Found {customer.Email}.",
    Invalid(var validationState) => validationState.ToString(),
    NotFound => "No customer has this email.",
    Conflict(var conflictMessage) => conflictMessage,
    DomainError(var reason) => reason,
    Canceled => "The search was canceled.",
    InternalError(var error) => $"Unexpected fault: {error.Code}",
    _ => "Unknown result."
};
```

Obey these rules:

- Put `Success<T>` first.
- Use the non-generic types for failures. A non-generic pattern also matches the generic type. Thus, one pattern works for all `T`.
- Keep a `_` branch. `IResult` is an open interface, so the compiler cannot prove that the switch is complete.

## Convert a failure to a different value type

A method that returns `IResult<TOut>` cannot return an `IResult<TIn>` failure directly.
Use `MapFailure<TOut>()`. It creates a failure of the same kind for the new value type and keeps the failure data.

```csharp
using DannyGoodacre.Primitives;
using Shop.Customers;

namespace Shop.Orders;

public sealed record Order(Guid Id, Guid CustomerId, decimal Total);

public sealed class OrderFactory(CustomerLookup customerLookup)
{
    public IResult<Order> Create(string customerEmail, decimal total)
    {
        IResult<Customer> customerResult = customerLookup.FindActive(customerEmail);

        if (customerResult is not Success<Customer>(var customer))
        {
            return customerResult.MapFailure<Order>();
        }

        return Result.Success(new Order(Guid.NewGuid(), customer.Id, total));
    }
}
```

| Source result | `MapFailure<TOut>()` returns |
| --- | --- |
| `Canceled` | `Canceled<TOut>` |
| `Conflict` | `Conflict<TOut>` with the same `Message` |
| `DomainError` | `DomainError<TOut>` with the same `Message` |
| `InternalError` | `InternalError<TOut>` with the same `Error` |
| `Invalid` | `Invalid<TOut>` with the same `ValidationState` instance |
| `NotFound` | `NotFound<TOut>` |
| `Success` or `Success<T>` | Throws `InvalidOperationException` |
| A custom `IResult` type | Throws `ArgumentOutOfRangeException` |

Call `MapFailure` only after you confirm that the result is a failure.

To return a typed result from a method that returns non-generic `IResult`, you do not need `MapFailure`. `IResult<T>` is also an `IResult`.

## Text output

| Type | `ToString()` output |
| --- | --- |
| `DomainError` and `DomainError<T>` | `DomainError: <Message>` |
| `InternalError` and `InternalError<T>` | `InternalError: <Error>` |
| Other types | The default record format, for example `NotFound { }` |

## Equality

Results are records, so they use value equality. Two results are equal when they have the same type and equal properties.

- `Result<int>.Success(5)` equals `Result<int>.Success(5)`.
- `Result.NotFound()` does not equal `Result<int>.NotFound()`. The types are different.
- `Invalid` compares its `ValidationState` by reference. `ValidationState` is a class with no value equality.

## Custom result types

Do not create custom `IResult` implementations. `MapFailure` and most consumers do not know them.
If a type implements `ISuccessResult`, `IsSuccess` returns `true` for it.

## Related documents

- [README.md](../README.md)
- [ERRORS.md](ERRORS.md)
- [VALIDATION.md](VALIDATION.md)
