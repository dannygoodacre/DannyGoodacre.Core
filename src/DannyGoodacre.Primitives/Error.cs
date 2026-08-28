namespace DannyGoodacre.Primitives;

public readonly record struct Error(string Message, Exception? Exception = null)
{
    public static implicit operator Error(string message) => new(message);

    public static implicit operator Error(Exception ex) => new(ex.Message, ex);
}
