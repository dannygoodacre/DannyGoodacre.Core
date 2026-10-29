using System.Text.Json.Serialization;

namespace DannyGoodacre.Primitives;

/// <summary>
/// Represents an application error containing a description, an optional error code, and an optional causal exception.
/// </summary>
public readonly record struct Error
{
    /// <summary>
    /// A human-readable description of the error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// A machine-readable category or error code. Defaults to "General".
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// The underlying causal exception if this error originated from an exception; otherwise, <see langword="null"/>.
    /// </summary>
    [JsonIgnore]
    public Exception? Exception { get; }

    /// <summary>
    /// Initialize a new instance of the <see cref="Error"/> struct with a message and default error code.
    /// </summary>
    public Error(string message)
        : this(message, code: "General", exception: null) { }

    /// <summary>
    /// Initialize a new instance of the <see cref="Error"/> struct with a message and specific error code.
    /// </summary>
    public Error(string message, string code)
        : this(message, code, exception: null) { }

    /// <summary>
    /// Initialize a new instance of the <see cref="Error"/> struct from an exception, using the exception type name as the code.
    /// </summary>
    public Error(Exception exception)
        : this(exception.Message, exception.GetType().Name, exception) { }

    /// <summary>
    /// Initialize a new instance of the <see cref="Error"/> struct from an exception with a custom error code.
    /// </summary>
    public Error(Exception exception, string code)
        : this(exception.Message, code, exception) { }

    /// <summary>
    /// Core constructor.
    /// </summary>
    [JsonConstructor]
    public Error(string message, string code = "General", Exception? exception = null)
    {
        Message = string.IsNullOrWhiteSpace(message) ? "An unspecified error occurred." : message;
        Code = string.IsNullOrWhiteSpace(code) ? "General" : code;
        Exception = exception;
    }

    public static implicit operator Error(string message) => new(message);

    public static implicit operator Error(Exception ex) => new(ex);

    public override string ToString()
        => Code is "General"
            ? Message
            : $"[{Code}] {Message}";
}
