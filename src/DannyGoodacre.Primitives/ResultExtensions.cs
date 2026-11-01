namespace DannyGoodacre.Primitives;

public static class ResultExtensions
{
    extension(IResult result)
    {
        /// <summary>
        /// Converts a non-success <see cref="IResult"/> into a strongly-typed failure
        /// <see cref="IResult{TOut}"/> carrying the original failure metadata.
        /// </summary>
        /// <typeparam name="TOut">The underlying value type of the target result.</typeparam>
        /// <returns>
        /// A new <see cref="IResult{TOut}"/> failure instance corresponding to the original failure state.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to map a successful result (<see cref="ISuccessResult"/>).
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="result"/> is an unrecognized failure implementation.
        /// </exception>
        public IResult<TOut> MapFailure<TOut>()
            => result switch
            {
                Canceled => new Canceled<TOut>(),
                Conflict conflict => new Conflict<TOut>(conflict.Message),
                DomainError domainError => new DomainError<TOut>(domainError.Message),
                InternalError internalError => new InternalError<TOut>(internalError.Error),
                Invalid invalid => new Invalid<TOut>(invalid.ValidationState),
                NotFound => new NotFound<TOut>(),

                ISuccessResult => throw new InvalidOperationException("Cannot map a successful result to a failure."),

                _ => throw new ArgumentOutOfRangeException(nameof(result), $"Unsupported result type '{result.GetType().Name}'.")
            };
    }
}
