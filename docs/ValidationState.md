# ValidationState

The `ValidationState` class provides a standardized structure for capturing and reporting data validation failures. It serves as the payload for the `Invalid` status from [Result](./Result.md), allowing the caller to see multiple errors across the different properties of the request.

## Usage

The following method is an example of this class being used to validate a command before executing the main business logic of the handler.

```csharp
protected override void Validate(ValidationState validationState, DoThingCommand command)
{
    if (string.IsNullOrWhiteSpace(command.Property))
    {
        validationState.AddError(nameof(command.Property), "Must not be null, empty, or whitespace.");
    }

    if (command.Property == "Something Bad")
    {
        validationState.AddError(nameof(command.Property), "Must not equal 'Something Bad'.");
    }
}
```

The command and query handlers are configured to automatically return an invalid response when the validation state object has any errors.
