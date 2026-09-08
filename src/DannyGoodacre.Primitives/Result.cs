namespace DannyGoodacre.Primitives;

/// <summary>
/// The outcome of an operation, without throwing exceptions.
/// </summary>
public interface IResult
{
    public bool IsSuccess => this is ISuccessResult;

    public bool IsFailure => !IsSuccess;
}

public interface ISuccessResult : IResult;

public sealed record Success : ISuccessResult;

public record Canceled : IResult;

public record Conflict(string Message) : IResult;

public record DomainError(string Message) : IResult;

public record InternalError(Error Error) : IResult;

public record Invalid(ValidationState ValidationState) : IResult;

public record NotFound : IResult;

/// <summary>
/// Static factory methods for creating <see cref="IResult"/> instances.
/// </summary>
public static class Result
{
    public static Success Success() => new();

    public static Success<T> Success<T>(T value) => new(value);

    public static Canceled Canceled() => new();

    public static Conflict Conflict(string message) => new(message);

    public static DomainError DomainError(string message) => new(message);

    public static InternalError InternalError(Error error) => new(error);

    public static Invalid Invalid(ValidationState state) => new(state);

    public static NotFound NotFound() => new();
}
