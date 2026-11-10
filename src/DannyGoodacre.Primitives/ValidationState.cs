using System.Collections.ObjectModel;
using System.Text;

namespace DannyGoodacre.Primitives;

/// <summary>
/// A mutable collection of validation errors grouped by property name.
/// </summary>
public sealed class ValidationState
{
    private Dictionary<string, List<string>>? _errors;

    private IReadOnlyDictionary<string, IReadOnlyList<string>>? _errorsView;

    /// <summary>
    /// A read-only view of the current validation errors. Key lookups are not case-sensitive.
    /// </summary>
    /// <remarks>
    /// The view is created on first read and kept until the next <see cref="AddError"/> call.
    /// The lists are read-only wrappers, so a caller cannot change the internal state.
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors
        => _errorsView ??= _errors is null
            ? ReadOnlyDictionary<string, IReadOnlyList<string>>.Empty
            : new ReadOnlyDictionary<string, IReadOnlyList<string>>(
                _errors.ToDictionary(
                    kvp => kvp.Key,
                    IReadOnlyList<string> (kvp) => kvp.Value.AsReadOnly(),
                    StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Indicates whether any validation errors have been recorded.
    /// </summary>
    public bool HasErrors => _errors is not null && _errors.Count > 0;

    /// <summary>
    /// Append a validation error message to the specified property name.
    /// </summary>
    public void AddError(string property, string error)
    {
        _errors ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        _errorsView = null;

        ref List<string>? list = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(
            _errors,
            property,
            out _);

        list ??= [];
        list.Add(error);
    }

    /// <summary>
    /// Merge another ValidationState instance into this instance.
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
    /// Format errors into a human-readable, multi-line string.
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

            foreach (string error in errors)
            {
                stringBuilder.AppendLine($"  - {error}");
            }
        }

        return stringBuilder.ToString().TrimEnd();
    }
}
