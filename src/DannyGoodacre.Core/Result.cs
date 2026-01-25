namespace DannyGoodacre.Core;

/// <summary>
/// The outcome of an operation, encapsulating its status, error, and validation state.
/// </summary>
public class Result
{
    public Status Status { get; internal init; }

    public string? Error { get; internal init; }

    public Exception? Exception { get; internal init; }

    public ValidationState? ValidationState { get; internal init; }

    public bool IsSuccess => Status == Status.Success;

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

    public static Result Cancelled()
        => new()
        {
            Status = Status.Cancelled
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

    public static Result InternalError(Exception exception) =>
        new()
        {
            Status = Status.InternalError,
            Exception = exception
        };

    public static Result<T> Success<T>(T value)
        => Result<T>.Success(value);

    public Result<T> ToResult<T>()
        => new()
        {
            Status = Status,
            Error = Error,
            Exception = Exception,
            ValidationState = ValidationState,
        };
}
