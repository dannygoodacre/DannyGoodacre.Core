using System.Diagnostics.CodeAnalysis;

namespace DannyGoodacre.Primitives;

/// <summary>
/// The outcome of an operation with a value, encapsulating its status, error, and validation state.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; private init; }

    [MemberNotNullWhen(true, nameof(Value))]
    public new bool IsSuccess => Status == Status.Success;

    private Result() { }

    public Result(Result result) : base(result) { }

    public static Result<T> Success(T value)
        => new()
        {
            Value = value,
            Status = Status.Success
        };

    public new static Result<T> Invalid(ValidationState validationState)
        => new()
        {
            Status = Status.Invalid,
            ValidationState = validationState
        };

    public new static Result<T> DomainError(string error)
        => new()
        {
            Status = Status.DomainError,
            Error = error
        };

    public new static Result<T> Canceled()
        => new()
        {
            Status = Status.Canceled
        };

    public new static Result<T> NotFound()
        => new()
        {
            Status = Status.NotFound
        };

    public new static Result<T> InternalError(string error)
        => new()
        {
            Status = Status.InternalError,
            Error = error
        };

    public new static Result<T> InternalError(Exception exception)
        => new()
        {
            Status = Status.InternalError,
            Exception = exception
        };
}
