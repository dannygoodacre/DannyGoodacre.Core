# Validation

`Invalid` carries a `ValidationState`, which collects validation errors grouped by property name.

| Member | Description |
| --- | --- |
| `ValidationError` | One error: a `Code`, a `Message`, and `Arguments` (never `null`, empty when none were given). Create it with `new ValidationError(message, code, arguments)`. The message and code are required, and a blank one throws `ArgumentException`; only `default(ValidationError)` has the code `Unknown` and a generic message. |
| `AddError(string property, ValidationError error)` | Adds an error to a property. A property can have several errors. |
| `AddErrorIf(bool condition, string property, ValidationError error)` | Calls `AddError` only when `condition` is `true`, and returns `condition`. |
| `HasErrors` | `true` if at least one error exists. |
| `Errors` | A read-only `IReadOnlyDictionary<string, IReadOnlyList<ValidationError>>` of the errors. |
| `Merge(ValidationState other)` | Copies all errors from `other` into this instance, after any existing errors for the same property. |
| `ToString()` | Returns the errors as multi-line text, or an empty string if there are none. |

- Property names are case-insensitive: `AddError("email", error)` adds to the same list as `AddError("Email", error)`, and keys keep the casing of the first call. Use `nameof` so names stay correct after a rename.
- `Errors` is read-only, so a caller cannot change the state through it.
- `ValidationState` is mutable and not thread-safe. Use one instance per validation operation.

Codes are the stable contract: a caller compares `Code` (and reads `Arguments`), while `Message` is formatted English for people and may change. The application owns its codes and wording; the library has no catalogue and does no message templating. Use PascalCase codes, such as `Required` and `InvalidFormat`. Messages are meant to be shown to users, for example in a toast, so keep internal detail, identifiers, and other users' data out of them; never interpolate raw input into one. `ValidationError` has no attempted value, because that can echo sensitive input. `Arguments` is a dictionary of strings, so the caller converts values itself; format numbers and dates with the invariant culture. It compares by content, so two errors with the same code, message, and arguments are equal.

```csharp
validationState.AddError(
    nameof(request.Name),
    new ValidationError(
        "Name must be at least 3 characters.",
        "TooShort",
        new Dictionary<string, string> { ["MinLength"] = "3" }));
```

Collect every error before returning, so the caller can show them all at once. The [README quick start](../README.md#1-return-results) shows this. To reuse rules for part of a larger request, validate the part into its own `ValidationState` and merge it:

```csharp
// AddressRules.Check validates an address and returns its errors in a new ValidationState.
validationState.Merge(AddressRules.Check(request.ShippingAddress, nameof(request.ShippingAddress)));
validationState.Merge(AddressRules.Check(request.BillingAddress, nameof(request.BillingAddress)));
```

`ToString()` returns one block per property, and prints only each `Message`:

```text
Email:
  - Email is required.
Password:
  - Password must contain at least 12 characters.
  - Password must contain a digit.
```

In `DannyGoodacre.Cqrs`, the pipeline passes one empty `ValidationState` to each `IValidator<T>` and returns `Invalid` if any errors were added. See [Validators](../../DannyGoodacre.Cqrs/docs/COMMANDS-AND-QUERIES.md#validators).
