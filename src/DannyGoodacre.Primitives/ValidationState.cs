using System.Text;

namespace DannyGoodacre.Primitives;

/// <summary>
/// A mutable collection of validation errors grouped by property name.
/// </summary>
public readonly record struct ValidationState()
{
    private readonly Dictionary<string, List<string>> _errors = [];

    /// <summary>
    /// A readonly view of the current validation errors.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors
        => _errors.ToDictionary(kvp => kvp.Key, IReadOnlyList<string> (kvp) => kvp.Value);

    /// <summary>
    /// Indicates whether any validation errors have been recorded.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Append a validation error message to the specified property name.
    /// </summary>
    public void AddError(string property, string error)
    {
        if (!_errors.TryGetValue(property, out List<string>? list))
        {
            list = [];

            _errors[property] = list;
        }

        list.Add(error);
    }

    /// <summary>
    /// A human-readable, multi-line string.
    /// </summary>
    public override string ToString()
    {
        if (!HasErrors)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();

        foreach ((string property, List<string> errors) in _errors)
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
