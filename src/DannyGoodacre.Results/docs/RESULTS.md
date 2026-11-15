# Results

Reference for creating and reading results and converting failures. The [README](../README.md#outcome-types) lists the outcome types; [ERRORS.md](ERRORS.md) and [VALIDATION.md](VALIDATION.md) describe the `Error` and `ValidationState` types that failures carry.

## Interfaces

| Interface | Purpose |
| --- | --- |
| `IResult` | The base contract for all results. Has `IsSuccess` and `IsFailure`. |
| `IResult<out T>` | A result that carries a `T` on success. Derives from `IResult`, and is covariant: an `IResult<string>` is also an `IResult<object>`. |
| `ISuccessResult` | A marker implemented by `Success` and `Success<T>`. |

All outcome types are records. Each generic failure type derives from its non-generic type, so `NotFound<T>` is also a `NotFound`. `Success<T>` does not derive from `Success`; both implement `ISuccessResult`.

### IsSuccess and IsFailure

`IsSuccess` is `true` when the result implements `ISuccessResult`; `IsFailure` is its opposite.

Both are default interface members, so C# only exposes them through a variable of an interface type. A constructor such as `new DomainError("...")` produces the concrete type, so declare the variable as `IResult` or `IResult<T>` rather than `var`:

```csharp
IResult result = new DomainError("The basket is empty.");

bool failed = result.IsFailure;
```

Method return values are already typed as `IResult` or `IResult<T>`, so they need no extra step.

## Create results

Create a result with its record constructor, for example `new NotFound<Customer>()` or `new Conflict<Guid>("...")`. In a method that returns `IResult<T>`, use the generic types, because a non-generic failure is not an `IResult<T>`.

```csharp
public IResult<Customer> FindActive(string email)
{
    Customer? customer = store.FindByEmail(email);

    if (customer is null)
    {
        return new NotFound<Customer>();
    }

    if (!customer.IsActive)
    {
        return new DomainError<Customer>("The customer account is closed.");
    }

    return new Success<Customer>(customer);
}
```

The static `Result` and `Result<T>` classes provide equivalent factory methods, such as `Result<Customer>.NotFound()`, which return `IResult` or `IResult<T>`.

## Read results

Use `IsSuccess` or `IsFailure` when you need neither the value nor the failure details, and a positional pattern on `Success<T>` to get the value:

```csharp
IResult<Customer> result = lookup.FindActive("jane@example.com");

if (result.IsFailure)
{
    return;
}

if (result is Success<Customer>(var customer))
{
    Console.WriteLine(customer.Email);
}
```

Use a `switch` expression to handle every outcome. Non-generic patterns such as `NotFound` also match the generic types, so one pattern works for every `T`:

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

## Convert a failure to a different value type

A method that returns `IResult<TOut>` cannot return an `IResult<TIn>` failure directly. `MapFailure<TOut>()` creates the same kind of failure for the new value type and keeps its data:

```csharp
public IResult<Order> Create(string customerEmail, decimal total)
{
    IResult<Customer> customerResult = customerLookup.FindActive(customerEmail);

    if (customerResult is not Success<Customer>(var customer))
    {
        return customerResult.MapFailure<Order>();
    }

    return new Success<Order>(new Order(Guid.NewGuid(), customer.Id, total));
}
```

Call `MapFailure` only after you confirm that the result is a failure; it throws for a success. A method that returns non-generic `IResult` does not need it, because `IResult<T>` is also an `IResult`.

## Equality

Results use record value equality.

- `new Success<int>(5)` equals `new Success<int>(5)`.
- `new NotFound()` does not equal `new NotFound<int>()`, because the types differ.
- `Invalid` compares its `ValidationState` by reference, because `ValidationState` is a class without value equality.
