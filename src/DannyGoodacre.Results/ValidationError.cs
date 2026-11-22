using System.Collections.ObjectModel;

namespace DannyGoodacre.Results;

/// <summary>
/// A single validation error that has a machine-readable code, a human-readable message,
/// and the arguments that were used to build the message.
/// </summary>
public readonly record struct ValidationError
{
    private const string DefaultMessage = "An unspecified error occurred.";

    // The message and code of default(ValidationError), which skips the constructor. They mark an error nobody created.
    private const string DefaultCode = "Unknown";

    /// <summary>
    /// Gets a human-readable description of the error, already formatted.
    /// </summary>
    /// <remarks>
    /// Intended to be shown to users, so it must not contain internal detail. Only <c>default(ValidationError)</c>
    /// has the generic message <c>"An unspecified error occurred."</c>
    /// </remarks>
    public string Message => field ?? DefaultMessage;

    /// <summary>
    /// Gets the machine-readable error code, such as <c>"Required"</c>.
    /// </summary>
    /// <remarks>
    /// Only <c>default(ValidationError)</c> has the code <c>"Unknown"</c>.
    /// </remarks>
    public string Code => field ?? DefaultCode;

    /// <summary>
    /// Gets the values behind the message, such as a limit, as strings.
    /// </summary>
    /// <remarks>
    /// Never <see langword="null"/>: empty when none were given. Format numbers and dates with the invariant culture.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Arguments => field ?? ReadOnlyDictionary<string, string>.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> struct.
    /// </summary>
    /// <param name="message">The human-readable description of the error.</param>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="arguments">The values behind the message. The dictionary is copied.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="message"/> or <paramref name="code"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    public ValidationError(string message, string code, IReadOnlyDictionary<string, string>? arguments = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Message = message;
        Code = code;

        Arguments = arguments is null or { Count: 0 }
            ? ReadOnlyDictionary<string, string>.Empty
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(arguments));
    }

    /// <summary>
    /// Compares the public values, with <see cref="Arguments"/> compared by content.
    /// </summary>
    /// <param name="other">The error to compare with.</param>
    /// <returns>
    /// <see langword="true"/> if the message and code are equal and <see cref="Arguments"/> has the same keys
    /// with equal values (ordinal); otherwise, <see langword="false"/>.
    /// </returns>
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
