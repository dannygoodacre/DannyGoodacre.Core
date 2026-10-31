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
    public override string ToString() => $"DomainError: {Message}";
}

public record InternalError(Error Error) : IResult
{
    /// <inheritdoc/>
    public override string ToString() => $"InternalError: {Error}";
}

public record Invalid(ValidationState ValidationState) : IResult;

public record NotFound : IResult;

/// <summary>
/// Provides static factory methods for creating <see cref="IResult"/> instances.
/// </summary>
public static class Result
{
    public static Success Success() => new();
    public static Success<T> Success<T>(T value) => new(value);
    public static Canceled Canceled() => new();
    public static Conflict Conflict(string message) => new(message);
    public static DomainError DomainError(string message) => new(message);
    public static InternalError InternalError(Error error) => new(error);
    public static InternalError InternalError(string message) => new(new Error(message));
    public static InternalError InternalError(Exception exception) => new(new Error(exception));
    public static Invalid Invalid(ValidationState state) => new(state);
    public static NotFound NotFound() => new();
}
