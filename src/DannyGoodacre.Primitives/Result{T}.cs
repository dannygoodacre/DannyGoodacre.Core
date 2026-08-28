namespace DannyGoodacre.Primitives;

public sealed record Success<T>(T Value) : Result<T>;

public sealed record DomainError<T>(Error Error) : Result<T>;

public abstract record Result<T>
{
    private protected Result() { }

    public bool IsSuccess => this is Success<T>;
    public bool IsFailure => !IsSuccess;

    public sealed record Canceled(Primitives.Canceled Result) : Result<T>;
    public sealed record NotFound(Primitives.NotFound Result) : Result<T>;
    public sealed record Conflict(Primitives.Conflict Result) : Result<T>;
    public sealed record DomainError(Primitives.DomainError Result) : Result<T>;
    public sealed record InternalError(Primitives.InternalError Result) : Result<T>;
    public sealed record Invalid(Primitives.Invalid Result) : Result<T>;

    public static implicit operator Result<T>(T value) => new Success<T>(value);

    public static implicit operator Result<T>(Primitives.Canceled result) => new Canceled(result);
    public static implicit operator Result<T>(Primitives.NotFound result) => new NotFound(result);
    public static implicit operator Result<T>(Primitives.Conflict result) => new Conflict(result);
    public static implicit operator Result<T>(Primitives.DomainError result) => new DomainError(result);
    public static implicit operator Result<T>(Primitives.InternalError result) => new InternalError(result);
    public static implicit operator Result<T>(Primitives.Invalid result) => new Invalid(result);
}
