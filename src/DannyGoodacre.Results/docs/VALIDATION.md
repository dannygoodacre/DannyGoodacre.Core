# Validation

`Invalid` carries a `ValidationState`, which collects validation errors grouped by property name.

| Member | Description |
| --- | --- |
| `AddError(string property, string error)` | Adds an error message to a property. A property can have several errors. |
| `HasErrors` | `true` if at least one error exists. |
| `Errors` | A read-only `IReadOnlyDictionary<string, IReadOnlyList<string>>` of the errors. |
| `Merge(ValidationState other)` | Copies all errors from `other` into this instance, after any existing errors for the same property. |
| `ToString()` | Returns the errors as multi-line text, or an empty string if there are none. |

- Property names are case-insensitive: `AddError("email", ...)` adds to the same list as `AddError("Email", ...)`, and keys keep the casing of the first call. Use `nameof` so names stay correct after a rename.
- `Errors` is read-only, so a caller cannot change the state through it.
- `ValidationState` is mutable and not thread-safe. Use one instance per validation operation.

Collect every error before returning, so the caller can show them all at once. The [README quick start](../README.md#1-return-results) shows this. To reuse rules for part of a larger request, validate the part into its own `ValidationState` and merge it:

```csharp
// AddressRules.Check validates an address and returns its errors in a new ValidationState.
validationState.Merge(AddressRules.Check(request.ShippingAddress, nameof(request.ShippingAddress)));
validationState.Merge(AddressRules.Check(request.BillingAddress, nameof(request.BillingAddress)));
```

`ToString()` returns one block per property:

```text
Email:
  - Email is required.
Password:
  - Password must contain at least 12 characters.
  - Password must contain a digit.
```

In `DannyGoodacre.Cqrs`, the pipeline passes one empty `ValidationState` to each `IValidator<T>` and returns `Invalid` if any errors were added. See [Validators](../../DannyGoodacre.Cqrs/docs/COMMANDS-AND-QUERIES.md#validators).
