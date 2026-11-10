namespace DannyGoodacre.Primitives;

/// <summary>
/// Defines the contract for an operation result carrying a <typeparamref name="T"/> payload.
/// </summary>
/// <typeparam name="T">The type of value returned when the operation succeeds.</typeparam>
public interface IResult<out T> : IResult;

/// <summary>
/// Represents a successful operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="Value">The value of the successful operation.</param>
public sealed record Success<T>(T Value) : ISuccessResult, IResult<T>;

/// <summary>
/// A <see cref="Canceled"/> result for a method that returns <see cref="IResult{T}"/>.
/// </summary>
/// <typeparam name="T">The value type of the result.</typeparam>
public sealed record Canceled<T> : Canceled, IResult<T>;

/// <summary>
/// A <see cref="Conflict"/> result for a method that returns <see cref="IResult{T}"/>.
/// </summary>
/// <typeparam name="T">The value type of the result.</typeparam>
/// <param name="Message">A description of the conflict.</param>
public sealed record Conflict<T>(string Message) : Conflict(Message), IResult<T>;

/// <summary>
/// A <see cref="DomainError"/> result for a method that returns <see cref="IResult{T}"/>.
/// </summary>
/// <typeparam name="T">The value type of the result.</typeparam>
/// <param name="Message">A description of the rule that stopped the operation.</param>
public sealed record DomainError<T>(string Message) : DomainError(Message), IResult<T>;

/// <summary>
/// A <see cref="NotFound"/> result for a method that returns <see cref="IResult{T}"/>.
/// </summary>
/// <typeparam name="T">The value type of the result.</typeparam>
public sealed record NotFound<T> : NotFound, IResult<T>;

/// <summary>
/// An <see cref="InternalError"/> result for a method that returns <see cref="IResult{T}"/>.
/// </summary>
/// <typeparam name="T">The value type of the result.</typeparam>
/// <param name="Error">The details of the fault.</param>
public sealed record InternalError<T>(Error Error) : InternalError(Error), IResult<T>;

/// <summary>
/// An <see cref="Invalid"/> result for a method that returns <see cref="IResult{T}"/>.
/// </summary>
/// <typeparam name="T">The value type of the result.</typeparam>
/// <param name="ValidationState">The validation errors, grouped by property name.</param>
public sealed record Invalid<T>(ValidationState ValidationState) : Invalid(ValidationState), IResult<T>;

/// <summary>
/// Provides static factory methods for creating <see cref="IResult{T}"/> instances.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public static class Result<T>
{
    /// <summary>
    /// Creates a <see cref="Success{T}"/> result that carries <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value of the successful operation.</param>
    public static IResult<T> Success(T value) => new Success<T>(value);

    /// <summary>
    /// Creates a <see cref="Canceled{T}"/> result.
    /// </summary>
    public static IResult<T> Canceled() => new Canceled<T>();

    /// <summary>
    /// Creates a <see cref="Conflict{T}"/> result.
    /// </summary>
    /// <param name="message">A description of the conflict.</param>
    public static IResult<T> Conflict(string message) => new Conflict<T>(message);

    /// <summary>
    /// Creates a <see cref="DomainError{T}"/> result.
    /// </summary>
    /// <param name="message">A description of the rule that stopped the operation.</param>
    public static IResult<T> DomainError(string message) => new DomainError<T>(message);

    /// <summary>
    /// Creates a <see cref="NotFound{T}"/> result.
    /// </summary>
    public static IResult<T> NotFound() => new NotFound<T>();

    /// <summary>
    /// Creates an <see cref="InternalError{T}"/> result.
    /// </summary>
    /// <param name="error">The details of the fault.</param>
    public static IResult<T> InternalError(Error error) => new InternalError<T>(error);

    /// <summary>
    /// Creates an <see cref="InternalError{T}"/> result with an <see cref="Error"/> that has this message
    /// and the default code.
    /// </summary>
    /// <param name="message">A description of the fault.</param>
    public static IResult<T> InternalError(string message) => new InternalError<T>(new Error(message));

    /// <summary>
    /// Creates an <see cref="InternalError{T}"/> result with an <see cref="Error"/> created from
    /// <paramref name="exception"/>, using the exception type name as the code.
    /// </summary>
    /// <param name="exception">The exception that caused the fault.</param>
    public static IResult<T> InternalError(Exception exception) => new InternalError<T>(new Error(exception));

    /// <summary>
    /// Creates an <see cref="Invalid{T}"/> result.
    /// </summary>
    /// <param name="state">The validation errors.</param>
    public static IResult<T> Invalid(ValidationState state) => new Invalid<T>(state);
}
