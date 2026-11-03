using System.Text.Json.Serialization;

namespace DannyGoodacre.Primitives;

/// <summary>
/// Represents an application error containing a description, a machine-readable error code,
/// and an optional causal exception.
/// </summary>
public readonly record struct Error
{
    private const string DefaultMessage = "An unspecified error occurred.";

    private const string DefaultCode = "General";

    // default(Error) does not run a constructor, so the fields can be null.
    // The properties return the default values in that case.
    private readonly string? _message;

    private readonly string? _code;

    /// <summary>
    /// Gets a human-readable description of the error.
    /// </summary>
    public string Message => _message ?? DefaultMessage;

    /// <summary>
    /// Gets a machine-readable category or error code. Defaults to <c>"General"</c>.
    /// </summary>
    public string Code => _code ?? DefaultCode;

    /// <summary>
    /// Gets the underlying causal exception if this error originated from an exception;
    /// otherwise, <see langword="null"/>.
    /// </summary>
    [JsonIgnore]
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> struct with a message and default error code.
    /// </summary>
    /// <param name="message">The human-readable description of the error.</param>
    public Error(string message)
        : this(message, code: "General", exception: null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> struct with a message and specific error code.
    /// </summary>
    /// <param name="message">The human-readable description of the error.</param>
    /// <param name="code">The machine-readable category or error code.</param>
    public Error(string message, string code)
        : this(message, code, exception: null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> struct from an exception,
    /// using the exception type name as the error code.
    /// </summary>
    /// <param name="exception">The underlying exception that caused the error.</param>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public Error(Exception exception)
        : this(NotNull(exception).Message, exception.GetType().Name, exception) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> struct from an exception with a custom error code.
    /// </summary>
    /// <param name="exception">The underlying exception that caused the error.</param>
    /// <param name="code">The machine-readable category or error code.</param>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public Error(Exception exception, string code)
        : this(NotNull(exception).Message, code, exception) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> struct with all error details.
    /// </summary>
    /// <param name="message">The human-readable description of the error.</param>
    /// <param name="code">The machine-readable category or error code.</param>
    /// <param name="exception">The optional underlying exception.</param>
    [JsonConstructor]
    public Error(string message, string code = "General", Exception? exception = null)
    {
        _message = string.IsNullOrWhiteSpace(message)
            ? DefaultMessage
            : message;

        _code = string.IsNullOrWhiteSpace(code)
            ? DefaultCode
            : code;

        Exception = exception;
    }

    /// <summary>
    /// Implicitly converts a message string into an <see cref="Error"/> with a default error code.
    /// </summary>
    public static implicit operator Error(string message) => new(message);

    /// <summary>
    /// Implicitly converts an exception into an <see cref="Error"/> using the exception type name as the error code.
    /// </summary>
    public static implicit operator Error(Exception exception) => new(exception);

    /// <summary>
    /// Compares the public values, so <c>default(Error)</c> equals an error with the default message and code.
    /// </summary>
    public bool Equals(Error other)
        => Message == other.Message
           && Code == other.Code
           && ReferenceEquals(Exception, other.Exception);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Message, Code, Exception);

    /// <inheritdoc/>
    public override string ToString()
        => Code is "General"
            ? Message
            : $"[{Code}] {Message}";

    private static Exception NotNull(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception;
    }
}
