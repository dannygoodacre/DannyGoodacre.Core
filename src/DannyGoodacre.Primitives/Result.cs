namespace DannyGoodacre.Primitives;

public interface IResult;
public interface IResult<T> : IResult;

public record Success : IResult;
public record Success<T>(T Value) : Success, IResult<T>;

public record Canceled : IResult;
public record Canceled<T> : Canceled, IResult<T>;

public record Conflict(Error Error) : IResult;
public record Conflict<T>(Error Error) : Conflict(Error), IResult<T>;

public record DomainError(Error Error) : IResult;
public record DomainError<T>(Error Error) : DomainError(Error), IResult<T>;

public record NotFound : IResult;
public record NotFound<T> : NotFound, IResult<T>;

public record InternalError(Error Error) : IResult;
public record InternalError<T>(Error Error) : InternalError(Error), IResult<T>;

public record Invalid(ValidationState ValidationState) : IResult;
public record Invalid<T>(ValidationState ValidationState) : Invalid(ValidationState), IResult<T>;

public static class Result
{
    public static Success Success() => new();
    public static Success<T> Success<T>(T value) => new(value);

    public static Canceled Canceled() => new();
    public static Canceled<T> Canceled<T>() => new();

    public static Conflict Conflict(Error error) => new(error);
    public static Conflict<T> Conflict<T>(Error error) => new(error);

    public static DomainError DomainError(Error error) => new(error);
    public static DomainError<T> DomainError<T>(Error error) => new(error);

    public static NotFound NotFound() => new();
    public static NotFound<T> NotFound<T>() => new();

    public static InternalError InternalError(Error error) => new(error);
    public static InternalError<T> InternalError<T>(Error error) => new(error);

    public static Invalid Invalid(ValidationState state) => new(state);
    public static Invalid Invalid<T>(ValidationState state) => new(state);
}
