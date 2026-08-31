namespace DannyGoodacre.Primitives;

public interface IResult
{
    public IResult<TOut> MapFailure<TOut>()
        => this switch
        {
            Canceled => new Canceled<TOut>(),

            Conflict conflict => new Conflict<TOut>(conflict.Error),

            DomainError domainError => new DomainError<TOut>(domainError.Error),

            InternalError internalError => new InternalError<TOut>(internalError.Error),

            Invalid invalid => new Invalid<TOut>(invalid.ValidationState),

            NotFound => new NotFound<TOut>(),

            Success => throw new InvalidOperationException("Cannot map a successful result to a failure."),

            _ => throw new ArgumentOutOfRangeException(nameof(IResult))
        };
}

public record Success : IResult;

public record Canceled : IResult;

public record Conflict(string Error) : IResult;

public record DomainError(string Error) : IResult;

public record InternalError(Error Error) : IResult;

public record Invalid(ValidationState ValidationState) : IResult;

public record NotFound : IResult;

public static class Result
{
    public static Success Success() => new();

    public static Success<T> Success<T>(T value) => new(value);

    public static Canceled Canceled() => new();

    public static Conflict Conflict(string error) => new(error);

    public static DomainError DomainError(string error) => new(error);

    public static InternalError InternalError(Error error) => new(error);

    public static Invalid Invalid(ValidationState state) => new(state);

    public static NotFound NotFound() => new();
}
