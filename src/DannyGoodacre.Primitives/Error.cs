namespace DannyGoodacre.Primitives;

/// <summary>
/// An error containing a description and an optional causal exception.
/// </summary>
/// <param name="Message">A human-readable description of the error.</param>
public readonly record struct Error(string Message)
{
    public static implicit operator Error(string message) => new(message);

    public static implicit operator Error(Exception ex) => new(ex.Message);
}
