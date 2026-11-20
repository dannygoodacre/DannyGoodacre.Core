using System.Collections.ObjectModel;
using System.Text;

namespace DannyGoodacre.Results;

/// <summary>
/// A mutable collection of validation errors grouped by property name.
/// </summary>
public sealed class ValidationState
{
    private Dictionary<string, List<ValidationError>>? _errors;

    private IReadOnlyDictionary<string, IReadOnlyList<ValidationError>>? _errorsView;

    /// <summary>
    /// Gets a read-only view of the current validation errors. Key lookups are not case-sensitive.
    /// </summary>
    /// <remarks>
    /// The view is created on first read and kept until the next <see cref="AddError(string, ValidationError)"/> call.
    /// The lists are read-only wrappers, so a caller cannot change the internal state.
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<ValidationError>> Errors
        => _errorsView ??= _errors is null
            ? ReadOnlyDictionary<string, IReadOnlyList<ValidationError>>.Empty
            : new ReadOnlyDictionary<string, IReadOnlyList<ValidationError>>(
                _errors.ToDictionary(
                    kvp => kvp.Key,
                    IReadOnlyList<ValidationError> (kvp) => kvp.Value.AsReadOnly(),
                    StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a value indicating whether any validation errors have been recorded.
    /// </summary>
    public bool HasErrors => _errors is not null && _errors.Count > 0;

    /// <summary>
    /// Appends a validation error to the specified property name.
    /// </summary>
    public void AddError(string property, ValidationError error)
    {
        _errors ??= new Dictionary<string, List<ValidationError>>(StringComparer.OrdinalIgnoreCase);

        _errorsView = null;

        ref List<ValidationError>? list = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(
            _errors,
            property,
            out _);

        list ??= [];
        list.Add(error);
    }

    /// <summary>
    /// Appends a validation error to the specified property name only when <paramref name="condition"/> is <see langword="true"/>.
    /// </summary>
    /// <returns><paramref name="condition"/>, so the caller knows whether an error was added.</returns>
    public bool AddErrorIf(bool condition, string property, ValidationError error)
    {
        if (condition)
        {
            AddError(property, error);
        }

        return condition;
    }

    /// <summary>
    /// Merges another <see cref="ValidationState"/> into this instance.
    /// </summary>
    public void Merge(ValidationState other)
    {
        if (other._errors is null)
        {
            return;
        }

        foreach (var (property, errors) in other._errors)
        {
            // Iterate a copy: when merging an instance into itself, AddError appends to this same list.
            foreach (var error in errors.ToArray())
            {
                AddError(property, error);
            }
        }
    }

    /// <summary>
    /// Formats errors into a human-readable, multi-line string.
    /// </summary>
    public override string ToString()
    {
        if (!HasErrors)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();

        foreach (var (property, errors) in _errors!)
        {
            stringBuilder.AppendLine($"{property}:");

            foreach (var error in errors)
            {
                stringBuilder.AppendLine($"  - {error.Message}");
            }
        }

        return stringBuilder.ToString().TrimEnd();
    }
}
