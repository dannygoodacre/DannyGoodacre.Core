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
    public static IResult<T> Success(T value) => new Success<T>(value);

    public static IResult<T> Canceled() => new Canceled<T>();

    public static IResult<T> Conflict(string message) => new Conflict<T>(message);

    public static IResult<T> DomainError(string message) => new DomainError<T>(message);

    public static IResult<T> NotFound() => new NotFound<T>();

    public static IResult<T> InternalError(Error error) => new InternalError<T>(error);

    public static IResult<T> InternalError(string message) => new InternalError<T>(new Error(message));

    public static IResult<T> InternalError(Exception exception) => new InternalError<T>(new Error(exception));

    public static IResult<T> Invalid(ValidationState state) => new Invalid<T>(state);
}
