# DannyGoodacre.Primitives

DannyGoodacre.Primitives is a .NET 10 library of result types for method return values.
A method returns a result object for each expected outcome. It does not throw an exception.

The package contains three parts:

- **Results.** `IResult`, `IResult<T>`, and seven outcome types.
- **Errors.** The `Error` struct. It describes an unexpected fault.
- **Validation.** The `ValidationState` class. It collects input errors by property name.

The package has no dependencies. `DannyGoodacre.Cqrs` uses it for all handler return values.

## Contents

| Document | Subject |
| --- | --- |
| [README.md](README.md) | Installation, quick start, and core concepts |
| [RESULTS.md](docs/RESULTS.md) | `IResult`, `IResult<T>`, outcome types, factory methods, pattern matching, and `MapFailure` |
| [ERRORS.md](docs/ERRORS.md) | The `Error` struct and `InternalError` |
| [VALIDATION.md](docs/VALIDATION.md) | `ValidationState` and `Invalid` |

## Installation

```shell
dotnet add package DannyGoodacre.Primitives
```

All types are in the `DannyGoodacre.Primitives` namespace.

## Core concepts

| Type | Outcome | Data |
| --- | --- | --- |
| `Success` / `Success<T>` | The operation completed. | `Value` (generic form only) |
| `DomainError` / `DomainError<T>` | A business rule stopped the operation. | `Message` |
| `NotFound` / `NotFound<T>` | The requested data does not exist. | None |
| `Conflict` / `Conflict<T>` | The current state prevents the operation. | `Message` |
| `Invalid` / `Invalid<T>` | The input is not valid. | `ValidationState` |
| `InternalError` / `InternalError<T>` | An unexpected fault occurred. | `Error` |
| `Canceled` / `Canceled<T>` | The operation was canceled. | None |

Each generic type derives from its non-generic type. For example, `NotFound<T>` is also a `NotFound`.

## Quick start

This example applies a discount code to an order total.

### 1. Return results from a method

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
            return Result<decimal>.Invalid(validationState);
        }

        DiscountCode? discount = repository.Find(code);

        if (discount is null)
        {
            return Result<decimal>.NotFound();
        }

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (discount.ExpiresOn < today)
        {
            return Result<decimal>.DomainError("The discount code has expired.");
        }

        decimal discountedTotal = Math.Round(orderTotal * (1 - discount.Percentage / 100m), 2);

        return Result<decimal>.Success(discountedTotal);
    }
}
```

### 2. Read the result

Use a `switch` expression. Put `Success<T>` first.

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

The non-generic patterns, for example `NotFound`, also match the generic types, for example `NotFound<decimal>`.

### 3. Test the method

```csharp
using DannyGoodacre.Primitives;
using Shop.Pricing;
using Xunit;

namespace Shop.Tests.Pricing;

public sealed class DiscountCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 9, 0, 0, TimeSpan.Zero);

    private static DiscountCalculator CreateCalculator(params DiscountCode[] codes)
        => new(new InMemoryDiscountCodeRepository(codes), new FixedTimeProvider(Now));

    [Fact]
    public void Apply_WhenCodeIsActive_ReturnsDiscountedTotal()
    {
        // Arrange
        var calculator = CreateCalculator(new DiscountCode("SPRING10", 10m, new DateOnly(2026, 4, 30)));

        // Act
        IResult<decimal> result = calculator.Apply(120.00m, "SPRING10");

        // Assert
        var success = Assert.IsType<Success<decimal>>(result);

        Assert.Equal(108.00m, success.Value);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Apply_WhenCodeHasExpired_ReturnsDomainError()
    {
        // Arrange
        var calculator = CreateCalculator(new DiscountCode("WINTER20", 20m, new DateOnly(2026, 2, 28)));

        // Act
        IResult<decimal> result = calculator.Apply(120.00m, "WINTER20");

        // Assert
        var domainError = Assert.IsType<DomainError<decimal>>(result);

        Assert.Equal("The discount code has expired.", domainError.Message);
    }

    [Fact]
    public void Apply_WhenCodeDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        IResult<decimal> result = calculator.Apply(120.00m, "UNKNOWN");

        // Assert
        Assert.IsType<NotFound<decimal>>(result);
    }

    [Fact]
    public void Apply_WhenInputIsNotValid_ReturnsAllErrors()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        IResult<decimal> result = calculator.Apply(0m, "");

        // Assert
        var invalid = Assert.IsType<Invalid<decimal>>(result);

        Assert.Equal(2, invalid.ValidationState.Errors.Count);
        Assert.Contains("orderTotal", invalid.ValidationState.Errors.Keys);
        Assert.Contains("code", invalid.ValidationState.Errors.Keys);
    }

    private sealed class InMemoryDiscountCodeRepository(IEnumerable<DiscountCode> codes) : IDiscountCodeRepository
    {
        private readonly Dictionary<string, DiscountCode> _codes = codes.ToDictionary(c => c.Code);

        public DiscountCode? Find(string code) => _codes.GetValueOrDefault(code);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
```

## Rules

1. Return a result for each expected outcome. Do not throw exceptions for business rules.
2. Return `Invalid` for input errors. Put all errors in one `ValidationState`.
3. Use `InternalError` only for unexpected faults.
4. Read `IsSuccess` and `IsFailure` through a variable of type `IResult` or `IResult<T>`. See [RESULTS.md](docs/RESULTS.md#issuccess-and-isfailure).

## Use with DannyGoodacre.Cqrs

`DannyGoodacre.Cqrs` handlers return these result types. The CQRS pipeline also creates some results, for example `Invalid` and `Canceled`.
See [RESULT-HANDLING.md](../DannyGoodacre.Cqrs/docs/RESULT-HANDLING.md) in the Cqrs package.
