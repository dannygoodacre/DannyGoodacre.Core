namespace DannyGoodacre.Primitives;

/// <summary>
/// The outcome of an operation, without throwing exceptions.
/// </summary>
public interface IResult
{
    public bool IsSuccess => this is Success;

    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Convert a non-success <see cref="IResult"/> to a strongly-typed failure <see cref="IResult{TOut}"/>.
    /// </summary>
    /// <typeparam name="TOut">The target payload type.</typeparam>
    /// <returns>A typed failure matching the current outcome.</returns>
    /// <exception cref="InvalidOperationException">Thrown when attempting to map a <see cref="Success"/> result.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown result is given.</exception>
    public IResult<TOut> MapFailure<TOut>()
        => this switch
        {
            Canceled => new Canceled<TOut>(),

            Conflict conflict => new Conflict<TOut>(conflict.Message),

            DomainError domainError => new DomainError<TOut>(domainError.Message),

            InternalError internalError => new InternalError<TOut>(internalError.Error),

            Invalid invalid => new Invalid<TOut>(invalid.ValidationState),

            NotFound => new NotFound<TOut>(),

            Success => throw new InvalidOperationException("Cannot map a successful result to a failure."),

            _ => throw new ArgumentOutOfRangeException(nameof(IResult))
        };
}

public record Success : IResult;

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
