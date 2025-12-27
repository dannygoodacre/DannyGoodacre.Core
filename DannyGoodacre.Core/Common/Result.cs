using System.Diagnostics.CodeAnalysis;

namespace DannyGoodacre.Core.Common;

/// <summary>
/// The outcome of an operation, encapsulating its status, error, and validation state.
/// </summary>
public class Result
{
    public Status Status { get; private init; }

    public string? Error { get; private init; }

    public Exception? Exception { get; private init; }

    public ValidationState? ValidationState { get; private init; }

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
}

/// <summary>
/// The outcome of an operation with a value, encapsulating its status, error, and validation state.
/// </summary>
public class Result<T>
{
    public T? Value { get; private init; }

    public Status Status { get; private init; }

    public string? Error { get; private init; }

    public Exception? Exception { get; private init; }

    public ValidationState? ValidationState { get; private init; }

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
}
