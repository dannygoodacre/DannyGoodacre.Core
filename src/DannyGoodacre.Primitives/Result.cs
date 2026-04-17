namespace DannyGoodacre.Primitives;

/// <summary>
/// The outcome of an operation, encapsulating its status, error, and validation state.
/// </summary>
public class Result
{
    public Status Status { get; protected init; }

    public string? Error { get; protected init; }

    public Exception? Exception { get; protected init; }

    public ValidationState? ValidationState { get; protected init; }

    public bool IsSuccess => Status == Status.Success;

    protected private Result() { }

    protected Result(Result result)
    {
        Status = result.Status;
        Error = result.Error;
        Exception = result.Exception;
        ValidationState = result.ValidationState;
    }

    public static Result Success()
        => new()
        {
            Status = Status.Success
        };

    public static Result Invalid(ValidationState validationState)
        => new()
        {
            Status = Status.Invalid,
            ValidationState = validationState
        };

    public static Result DomainError(string error)
        => new()
        {
            Status = Status.DomainError,
            Error = error
        };

    public static Result Canceled()
        => new()
        {
            Status = Status.Canceled
        };

    public static Result NotFound()
        => new()
        {
            Status = Status.NotFound
        };

    public static Result InternalError(string error)
        => new()
        {
            Status = Status.InternalError,
            Error = error
        };

    public static Result InternalError(Exception exception)
        => new()
        {
            Status = Status.InternalError,
            Exception = exception
        };

    public static Result<T> Success<T>(T value)
        => Result<T>.Success(value);
}
