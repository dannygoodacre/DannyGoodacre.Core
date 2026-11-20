using System.Collections.ObjectModel;

namespace DannyGoodacre.Results;

/// <summary>
/// Represents a single validation error: a machine-readable code, a human-readable message,
/// and the arguments that were used to build the message.
/// </summary>
public readonly record struct ValidationError
{
    private const string DefaultMessage = "An unspecified error occurred.";

    private const string DefaultCode = "General";

    // default(ValidationError) does not run a constructor, so the fields can be null.
    // The properties return the default values in that case.
    private readonly string? _message;

    private readonly string? _code;

    private readonly IReadOnlyDictionary<string, string>? _arguments;

    /// <summary>
    /// Gets a human-readable description of the error, already formatted. Defaults to a generic message.
    /// </summary>
    public string Message => _message ?? DefaultMessage;

    /// <summary>
    /// Gets the machine-readable error code, such as <c>"Required"</c>. Defaults to <c>"General"</c>.
    /// </summary>
    public string Code => _code ?? DefaultCode;

    /// <summary>
    /// Gets the values behind the message, such as a limit. Never <see langword="null"/>: empty when none were given.
    /// </summary>
    public IReadOnlyDictionary<string, string> Arguments => _arguments ?? ReadOnlyDictionary<string, string>.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> struct.
    /// </summary>
    /// <param name="message">The human-readable description of the error.</param>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="arguments">The values behind the message. The dictionary is copied.</param>
    public ValidationError(
        string message,
        string code = DefaultCode,
        IReadOnlyDictionary<string, string>? arguments = null)
    {
        _message = string.IsNullOrWhiteSpace(message)
            ? DefaultMessage
            : message;

        _code = string.IsNullOrWhiteSpace(code)
            ? DefaultCode
            : code;

        _arguments = arguments is null or { Count: 0 }
            ? null
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(arguments));
    }

    /// <summary>
    /// Compares the public values, with <see cref="Arguments"/> compared by content: the same keys,
    /// and equal values (ordinal).
    /// </summary>
    public bool Equals(ValidationError other)
    {
        if (Message != other.Message || Code != other.Code || Arguments.Count != other.Arguments.Count)
        {
            return false;
        }

        foreach (var (key, value) in Arguments)
        {
            if (!other.Arguments.TryGetValue(key, out string? otherValue) || value != otherValue)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Summed, so the hash does not depend on the order of the arguments.
        var argumentsHash = 0;

        foreach (var (key, value) in Arguments)
        {
            argumentsHash += HashCode.Combine(key, value);
        }

        return HashCode.Combine(Message, Code, argumentsHash);
    }

    /// <inheritdoc/>
    public override string ToString() => Message;
}
