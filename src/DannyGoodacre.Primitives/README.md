# DannyGoodacre.Primitives

DannyGoodacre.Primitives is a dependency-free .NET 10 library of result types. A method returns a result object for each expected outcome instead of throwing an exception.

The package has three parts:

- **Results.** `IResult`, `IResult<T>`, and seven outcome types.
- **Errors.** The `Error` struct, which describes an unexpected fault.
- **Validation.** The `ValidationState` class, which collects input errors by property name.

`DannyGoodacre.Cqrs` uses these types for all handler return values.

The reference docs are [RESULTS.md](docs/RESULTS.md) (creating and reading results, `MapFailure`, and equality), [ERRORS.md](docs/ERRORS.md) (`Error`), and [VALIDATION.md](docs/VALIDATION.md) (`ValidationState`).

## Installation

```shell
dotnet add package DannyGoodacre.Primitives
```

> **Note:** The package is currently published to GitHub Packages only; publication to nuget.org is planned. Until then, add the feed with a GitHub token that has the `read:packages` scope:
>
> ```shell
> dotnet nuget add source https://nuget.pkg.github.com/dannygoodacre/index.json --name dannygoodacre --username <github-username> --password <token>
> ```

All types are in the `DannyGoodacre.Primitives` namespace.

## Outcome types

| Type | Outcome | Data |
| --- | --- | --- |
| `Success` / `Success<T>` | The operation completed. | `Value` (generic form only) |
| `DomainError` / `DomainError<T>` | A business rule stopped the operation. | `Message` |
| `NotFound` / `NotFound<T>` | The requested data does not exist. | None |
| `Conflict` / `Conflict<T>` | The current state prevents the operation. | `Message` |
| `Invalid` / `Invalid<T>` | The input is not valid. | `ValidationState` |
| `InternalError` / `InternalError<T>` | An unexpected fault occurred. | `Error` |
| `Canceled` / `Canceled<T>` | The operation was canceled. | None |

Each generic failure type derives from its non-generic type, so `NotFound<T>` is also a `NotFound`.

## Quick start

This example applies a discount code to an order total.

### 1. Return results

```csharp
using DannyGoodacre.Primitives;

namespace Shop.Pricing;

public sealed record DiscountCode(string Code, decimal Percentage, DateOnly ExpiresOn);

public interface IDiscountCodeRepository
{
    DiscountCode? Find(string code);
}

public sealed class DiscountCalculator(IDiscountCodeRepository repository, TimeProvider timeProvider)
{
    public IResult<decimal> Apply(decimal orderTotal, string code)
    {
        var validationState = new ValidationState();

        if (orderTotal <= 0)
        {
            validationState.AddError(nameof(orderTotal), "Order total must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            validationState.AddError(nameof(code), "Discount code is required.");
        }

        if (validationState.HasErrors)
        {
            return new Invalid<decimal>(validationState);
        }

        DiscountCode? discount = repository.Find(code);

        if (discount is null)
        {
            return new NotFound<decimal>();
        }

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (discount.ExpiresOn < today)
        {
            return new DomainError<decimal>("The discount code has expired.");
        }

        decimal discountedTotal = Math.Round(orderTotal * (1 - discount.Percentage / 100m), 2);

        return new Success<decimal>(discountedTotal);
    }
}
```

### 2. Read the result

Use a `switch` expression to handle every outcome. Non-generic patterns such as `NotFound` also match the generic types such as `NotFound<decimal>`.

```csharp
IResult<decimal> result = calculator.Apply(120.00m, "SPRING10");

string message = result switch
{
    Success<decimal>(var total) => $"New total: {total:C}",
    Invalid(var validationState) => $"Correct these errors:{Environment.NewLine}{validationState}",
    NotFound => "The discount code does not exist.",
    DomainError(var reason) => reason,
    _ => "The discount could not be applied."
};
```

### 3. Test the method

Assert on the concrete result type:

```csharp
IResult<decimal> result = calculator.Apply(0m, "");

var invalid = Assert.IsType<Invalid<decimal>>(result);
Assert.Equal(2, invalid.ValidationState.Errors.Count);
```

The [DannyGoodacre.Testing](../DannyGoodacre.Testing/README.md) package adds assertion helpers that do the type check and return the contents, for example `ValidationState state = result.ShouldBeInvalid();`.

## Recommended usage

- **Model each expected outcome as a result.** A business rule that stops an operation is easier for callers to handle as a `DomainError` than as an exception.
- **Collect input errors together.** Returning one `Invalid` with every error in its `ValidationState` lets the caller show them all at once.
- **Keep `InternalError` for the unexpected.** That way, an `InternalError` in the logs always points to a fault worth investigating.
- **Read `IsSuccess` and `IsFailure` through an interface type.** They are only visible on a variable typed as `IResult` or `IResult<T>`. See [RESULTS.md](docs/RESULTS.md#issuccess-and-isfailure).

## Use with DannyGoodacre.Cqrs

`DannyGoodacre.Cqrs` handlers return these types. See [COMMANDS-AND-QUERIES.md](../DannyGoodacre.Cqrs/docs/COMMANDS-AND-QUERIES.md#handlers).
