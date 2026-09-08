namespace DannyGoodacre.Primitives;

public static class ResultExtensions
{
    /// <summary>
    /// Convert a non-success <see cref="IResult"/> to a strongly-typed failure <see cref="IResult{TOut}"/>.
    /// </summary>
    public static IResult<TOut> MapFailure<TOut>(this IResult result)
        => result switch
        {
            Canceled => new Canceled<TOut>(),
            Conflict conflict => new Conflict<TOut>(conflict.Message),
            DomainError domainError => new DomainError<TOut>(domainError.Message),
            InternalError internalError => new InternalError<TOut>(internalError.Error),
            Invalid invalid => new Invalid<TOut>(invalid.ValidationState),
            NotFound => new NotFound<TOut>(),

            ISuccessResult => throw new InvalidOperationException("Cannot map a successful result to a failure."),

            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };
}
