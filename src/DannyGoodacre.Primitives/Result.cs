namespace DannyGoodacre.Primitives;

public record Success;

public record Canceled;

public record NotFound;

public record Conflict(Error Error);

public record DomainError(Error Error);

public record InternalError(Error Error);

public record Invalid(ValidationState ValidationState);

public abstract record Result
{
    protected private Result() { }

    public bool IsSuccess => this is Success;

    public bool IsFailure => !IsSuccess;

    public sealed record Success : Result;

    public sealed record Canceled(Primitives.Canceled Result) : Result;

    public sealed record NotFound(Primitives.NotFound Result) : Result;

    public sealed record Conflict(Primitives.Conflict Result) : Result;

    public sealed record DomainError(Primitives.DomainError Result) : Result;

    public sealed record InternalError(Primitives.InternalError Result) : Result;

    public sealed record Invalid(Primitives.Invalid Result) : Result;

    public static implicit operator Result(Primitives.Canceled result) => new Canceled(result);

    public static implicit operator Result(Primitives.NotFound result) => new NotFound(result);

    public static implicit operator Result(Primitives.Conflict result) => new Conflict(result);

    public static implicit operator Result(Primitives.DomainError result) => new DomainError(result);

    public static implicit operator Result(Primitives.InternalError result) => new InternalError(result);

    public static implicit operator Result(Primitives.Invalid result) => new Invalid(result);
}
