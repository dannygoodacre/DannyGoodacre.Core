# Validation

This document describes the `ValidationState` class and the `Invalid` result.

## ValidationState

`ValidationState` collects validation errors. It groups the errors by property name.
Each property can have more than one error.

| Member | Description |
| --- | --- |
| `AddError(string property, string error)` | Adds an error message to a property. |
| `HasErrors` | `true` if one or more errors exist. |
| `Errors` | A read-only view of the errors. The type is `IReadOnlyDictionary<string, IReadOnlyList<string>>`. Lookups are not case-sensitive. |
| `Merge(ValidationState other)` | Adds all errors from `other` to this instance. |
| `ToString()` | Returns the errors as text with one line for each error. |

`ValidationState` is mutable and is not thread-safe. Use one instance for one validation operation.

## Add errors

```csharp
var validationState = new ValidationState();

validationState.AddError("Email", "Email is required.");
validationState.AddError("Password", "Password must contain at least 12 characters.");
validationState.AddError("Password", "Password must contain a digit.");
```

Property names are not case-sensitive. `AddError("email", ...)` adds to the same list as `AddError("Email", ...)`.
The list keeps the casing of the first call.

Use `nameof` for the property name. Then the name stays correct when you rename the property.

## Read errors

```csharp
if (validationState.HasErrors)
{
    foreach ((string property, IReadOnlyList<string> messages) in validationState.Errors)
    {
        Console.WriteLine($"{property}: {string.Join(" ", messages)}");
    }
}
```

Know these facts about `Errors`:

- Key lookups are not case-sensitive. After `AddError("Email", ...)`, `Errors["email"]` returns the same list.
- The keys keep the casing of the first `AddError` call for each property.
- The view is created on the first read and kept until the next `AddError` call.
- The lists are read-only. A caller cannot change the errors through `Errors`.

### Text output

For the errors above, `ToString()` returns one block for each property:

```text
Email:
  - Email is required.
Password:
  - Password must contain at least 12 characters.
  - Password must contain a digit.
```

If there are no errors, `ToString()` returns an empty string.

## Return Invalid

Put all errors in one `ValidationState`. Then return `Invalid` with that instance.
Do not stop at the first error. The caller can then show all errors at one time.

```csharp
using DannyGoodacre.Primitives;

namespace Shop.Accounts;

public sealed record SignUpRequest(string Email, string Password, DateOnly DateOfBirth);

public sealed class SignUpValidator(TimeProvider timeProvider)
{
    public IResult Validate(SignUpRequest request)
    {
        var validationState = new ValidationState();

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            validationState.AddError(nameof(request.Email), "A valid email address is required.");
        }

        if (request.Password.Length < 12)
        {
            validationState.AddError(nameof(request.Password), "Password must contain at least 12 characters.");
        }

        if (!request.Password.Any(char.IsDigit))
        {
            validationState.AddError(nameof(request.Password), "Password must contain a digit.");
        }

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (request.DateOfBirth.AddYears(18) > today)
        {
            validationState.AddError(nameof(request.DateOfBirth), "You must be 18 or older.");
        }

        return validationState.HasErrors
            ? Result.Invalid(validationState)
            : Result.Success();
    }
}
```

The ternary expression compiles because `IResult` is the target type.

## Combine validators with Merge

Use `Merge` to reuse a validator for a part of a larger request.

```csharp
using DannyGoodacre.Primitives;

namespace Shop.Orders;

public sealed record Address(string Line1, string City, string PostalCode);

public sealed record CheckoutRequest(Guid BasketId, Address ShippingAddress, Address BillingAddress);

public static class AddressRules
{
    public static ValidationState Check(Address address, string prefix)
    {
        var validationState = new ValidationState();

        if (string.IsNullOrWhiteSpace(address.Line1))
        {
            validationState.AddError($"{prefix}.{nameof(address.Line1)}", "Address line 1 is required.");
        }

        if (string.IsNullOrWhiteSpace(address.City))
        {
            validationState.AddError($"{prefix}.{nameof(address.City)}", "City is required.");
        }

        if (string.IsNullOrWhiteSpace(address.PostalCode))
        {
            validationState.AddError($"{prefix}.{nameof(address.PostalCode)}", "Postal code is required.");
        }

        return validationState;
    }
}

public static class CheckoutRules
{
    public static IResult Check(CheckoutRequest request)
    {
        var validationState = new ValidationState();

        if (request.BasketId == Guid.Empty)
        {
            validationState.AddError(nameof(request.BasketId), "Basket ID is required.");
        }

        validationState.Merge(AddressRules.Check(request.ShippingAddress, nameof(request.ShippingAddress)));
        validationState.Merge(AddressRules.Check(request.BillingAddress, nameof(request.BillingAddress)));

        return validationState.HasErrors
            ? Result.Invalid(validationState)
            : Result.Success();
    }
}
```

`Merge` copies the errors. It does not keep a reference to the other instance.
If both instances have errors for the same property, `Merge` adds the new errors after the existing errors.

## Test validation

```csharp
using DannyGoodacre.Primitives;
using Shop.Orders;
using Xunit;

namespace Shop.Tests.Orders;

public sealed class CheckoutRulesTests
{
    private static readonly Address ValidAddress = new("1 High Street", "York", "YO1 7HH");

    [Fact]
    public void Check_WhenRequestIsValid_ReturnsSuccess()
    {
        // Arrange
        var request = new CheckoutRequest(Guid.NewGuid(), ValidAddress, ValidAddress);

        // Act
        IResult result = CheckoutRules.Check(request);

        // Assert
        Assert.IsType<Success>(result);
    }

    [Fact]
    public void Check_WhenShippingAddressIsEmpty_ReturnsPrefixedErrors()
    {
        // Arrange
        var request = new CheckoutRequest(Guid.NewGuid(), new Address("", "", ""), ValidAddress);

        // Act
        IResult result = CheckoutRules.Check(request);

        // Assert
        var invalid = Assert.IsType<Invalid>(result);

        IReadOnlyDictionary<string, IReadOnlyList<string>> errors = invalid.ValidationState.Errors;

        Assert.Equal(3, errors.Count);
        Assert.Contains("ShippingAddress.Line1", errors.Keys);
        Assert.Contains("ShippingAddress.City", errors.Keys);
        Assert.Contains("ShippingAddress.PostalCode", errors.Keys);
    }
}
```

## Use with DannyGoodacre.Cqrs

In `DannyGoodacre.Cqrs`, an `IValidator<T>` receives an empty `ValidationState` from the pipeline.
The validator adds errors. The pipeline returns `Invalid` if `HasErrors` is `true`.
See [ARCHITECTURE.md](../../DannyGoodacre.Cqrs/docs/ARCHITECTURE.md#validation-short-circuit) in the Cqrs package.

## Related documents

- [README.md](../README.md)
- [RESULTS.md](RESULTS.md)
- [ERRORS.md](ERRORS.md)
