namespace DannyGoodacre.Primitives;

/// <summary>
/// Defines the contract for an operation result without throwing exceptions.
/// </summary>
public interface IResult
{
    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => this is ISuccessResult;

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;
}

/// <summary>
/// Marker interface representing a successful operation outcome.
/// </summary>
public interface ISuccessResult : IResult;

/// <summary>
/// Represents a successful operation without a return payload.
/// </summary>
public sealed record Success : ISuccessResult;

/// <summary>
/// Represents an operation that was canceled before it completed.
/// </summary>
public record Canceled : IResult;

/// <summary>
/// Represents an operation that the current state prevents, for example a duplicate key or a stale version.
/// </summary>
/// <param name="Message">A description of the conflict.</param>
public record Conflict(string Message) : IResult;

/// <summary>
/// Represents an operation that a business rule stopped.
/// </summary>
/// <param name="Message">A description of the rule that stopped the operation.</param>
public record DomainError(string Message) : IResult
{
    /// <inheritdoc/>
    /// <remarks>Sealed, so that <see cref="DomainError{T}"/> uses the same format.</remarks>
    public sealed override string ToString() => $"DomainError: {Message}";
}

/// <summary>
/// Represents an operation that failed because of an unexpected fault.
/// </summary>
/// <param name="Error">The details of the fault.</param>
public record InternalError(Error Error) : IResult
{
    /// <inheritdoc/>
    /// <remarks>Sealed, so that <see cref="InternalError{T}"/> uses the same format.</remarks>
    public sealed override string ToString() => $"InternalError: {Error}";
}

/// <summary>
/// Represents an operation whose input is not valid.
/// </summary>
/// <param name="ValidationState">The validation errors, grouped by property name.</param>
public record Invalid(ValidationState ValidationState) : IResult;

/// <summary>
/// Represents an operation whose requested data does not exist.
/// </summary>
public record NotFound : IResult;

/// <summary>
/// Provides static factory methods for creating <see cref="IResult"/> instances.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a <see cref="Primitives.Success"/> result.
    /// </summary>
    public static IResult Success() => new Success();

    /// <summary>
    /// Creates a <see cref="Success{T}"/> result that carries <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value of the successful operation.</param>
    public static IResult<T> Success<T>(T value) => new Success<T>(value);

    /// <summary>
    /// Creates a <see cref="Primitives.Canceled"/> result.
    /// </summary>
    public static IResult Canceled() => new Canceled();

    /// <summary>
    /// Creates a <see cref="Primitives.Conflict"/> result.
    /// </summary>
    /// <param name="message">A description of the conflict.</param>
    public static IResult Conflict(string message) => new Conflict(message);

    /// <summary>
    /// Creates a <see cref="Primitives.DomainError"/> result.
    /// </summary>
    /// <param name="message">A description of the rule that stopped the operation.</param>
    public static IResult DomainError(string message) => new DomainError(message);

    /// <summary>
    /// Creates an <see cref="Primitives.InternalError"/> result.
    /// </summary>
    /// <param name="error">The details of the fault.</param>
    public static IResult InternalError(Error error) => new InternalError(error);

    /// <summary>
    /// Creates an <see cref="Primitives.InternalError"/> result with an <see cref="Error"/> that has this message
    /// and the default code.
    /// </summary>
    /// <param name="message">A description of the fault.</param>
    public static IResult InternalError(string message) => new InternalError(new Error(message));

    /// <summary>
    /// Creates an <see cref="Primitives.InternalError"/> result with an <see cref="Error"/> created from
    /// <paramref name="exception"/>, using the exception type name as the code.
    /// </summary>
    /// <param name="exception">The exception that caused the fault.</param>
    public static IResult InternalError(Exception exception) => new InternalError(new Error(exception));

    /// <summary>
    /// Creates an <see cref="Primitives.Invalid"/> result.
    /// </summary>
    /// <param name="state">The validation errors.</param>
    public static IResult Invalid(ValidationState state) => new Invalid(state);

    /// <summary>
    /// Creates a <see cref="Primitives.NotFound"/> result.
    /// </summary>
    public static IResult NotFound() => new NotFound();
}
