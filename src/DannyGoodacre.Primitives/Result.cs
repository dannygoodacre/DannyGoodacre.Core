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

public record Canceled : IResult;

public record Conflict(string Message) : IResult;

public record DomainError(string Message) : IResult
{
    /// <inheritdoc/>
    /// <remarks>Sealed, so that <see cref="DomainError{T}"/> uses the same format.</remarks>
    public sealed override string ToString() => $"DomainError: {Message}";
}

public record InternalError(Error Error) : IResult
{
    /// <inheritdoc/>
    /// <remarks>Sealed, so that <see cref="InternalError{T}"/> uses the same format.</remarks>
    public sealed override string ToString() => $"InternalError: {Error}";
}

public record Invalid(ValidationState ValidationState) : IResult;

public record NotFound : IResult;

/// <summary>
/// Provides static factory methods for creating <see cref="IResult"/> instances.
/// </summary>
public static class Result
{
    public static IResult Success() => new Success();
    public static IResult<T> Success<T>(T value) => new Success<T>(value);
    public static IResult Canceled() => new Canceled();
    public static IResult Conflict(string message) => new Conflict(message);
    public static IResult DomainError(string message) => new DomainError(message);
    public static IResult InternalError(Error error) => new InternalError(error);
    public static IResult InternalError(string message) => new InternalError(new Error(message));
    public static IResult InternalError(Exception exception) => new InternalError(new Error(exception));
    public static IResult Invalid(ValidationState state) => new Invalid(state);
    public static IResult NotFound() => new NotFound();
}
