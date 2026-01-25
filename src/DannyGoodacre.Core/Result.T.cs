using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DannyGoodacre.Core;

/// <summary>
/// The outcome of an operation with a value, encapsulating its status, error, and validation state.
/// </summary>
public class Result<T>
{
    public T? Value { get; private init; }

    public Status Status { get; internal init; }

    public string? Error { get; internal init; }

    public Exception? Exception { get; internal init; }

    public ValidationState? ValidationState { get; internal init; }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess => Status == Status.Success;

    public static Result<T> Success(T value)
        => new()
        {
            Value = value,
            Status = Status.Success
        };

    public static Result<T> Invalid(ValidationState validationState)
        => new()
        {
            Status = Status.Invalid,
            ValidationState = validationState
        };

    public static Result<T> DomainError(string error)
        => new()
        {
            Status = Status.DomainError,
            Error = error
        };

    public static Result<T> Cancelled()
        => new()
        {
            Status = Status.Cancelled
        };

    public static Result<T> NotFound()
        => new()
        {
            Status = Status.NotFound
        };

    public static Result<T> InternalError(string error)
        => new()
        {
            Status = Status.InternalError,
            Error = error
        };

    public static Result<T> InternalError(Exception exception)
        => new()
        {
            Status = Status.InternalError,
            Exception = exception
        };

    public Result ToVoidResult()
        => new()
        {
            Status = Status,
            Error = Error,
            Exception = Exception,
            ValidationState = ValidationState,
        };
}
