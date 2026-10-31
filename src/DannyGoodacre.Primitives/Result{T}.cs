namespace DannyGoodacre.Primitives;

/// <summary>
/// Defines the contract for an operation result carrying a <typeparamref name="T"/> payload.
/// </summary>
/// <typeparam name="T">The type of value returned when the operation succeeds.</typeparam>
public interface IResult<out T> : IResult;

public sealed record Success<T>(T Value) : ISuccessResult, IResult<T>;

public sealed record Canceled<T> : Canceled, IResult<T>;

public sealed record Conflict<T>(string Message) : Conflict(Message), IResult<T>;

public sealed record DomainError<T>(string Message) : DomainError(Message), IResult<T>;

public sealed record NotFound<T> : NotFound, IResult<T>;

public sealed record InternalError<T>(Error Error) : InternalError(Error), IResult<T>;

public sealed record Invalid<T>(ValidationState ValidationState) : Invalid(ValidationState), IResult<T>;

/// <summary>
/// Provides static factory methods for creating <see cref="IResult{T}"/> instances.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public static class Result<T>
{
    public static Success<T> Success(T value) => new(value);

    public static Canceled<T> Canceled() => new();

    public static Conflict<T> Conflict(string message) => new(message);

    public static DomainError<T> DomainError(string message) => new(message);

    public static NotFound<T> NotFound() => new();

    public static InternalError<T> InternalError(Error error) => new(error);

    public static InternalError<T> InternalError(string message) => new(new Error(message));

    public static InternalError<T> InternalError(Exception exception) => new(new Error(exception));

    public static Invalid<T> Invalid(ValidationState state) => new(state);
}
